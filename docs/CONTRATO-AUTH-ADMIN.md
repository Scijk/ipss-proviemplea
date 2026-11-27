# Contrato inicial: autenticación y validación municipal

Primer corte implementado en ASP.NET Core. El API queda bajo `/api/v1`, consume y responde JSON, y conserva los nombres de campos principales del API legado. La referencia de origen y sus diferencias pendientes están en [MIGRATION-INVENTORY.md](MIGRATION-INVENTORY.md).

La interfaz Razor incluye `/login`, `/registro/talento`, `/registro/empresa`, `/talento/perfil`, `/talento/solicitudes`, `/empresa/perfil`, `/empresa/vitrina`, `/empresa/solicitudes`, `/admin/talentos` y `/admin/solicitudes`. Las rutas de módulos requieren autenticación y el rol correspondiente.

## Endpoints

| Método y ruta | Acceso | Respuesta satisfactoria |
|---|---|---|
| `POST /api/v1/auth/login` | Público; 10 intentos por minuto/IP | `200` con JWT y usuario; `401` con credenciales inválidas. |
| `POST /api/v1/auth/register/talento` | Público | `201`; queda en estado `Pendiente`. Correo duplicado: `409`. |
| `POST /api/v1/auth/register/empresa` | Público | `201`; crea usuario, empresa y asociación en una transacción. Correo/RUT duplicado: `409`; catálogos inexistentes: `400`. Requiere `id_rubro`, `id_tipo_empresa`, `nombre_contacto` y `telefono_contacto`; la empresa queda aprobada automáticamente en esta iteración. |
| `GET /api/v1/admin/usuarios` | JWT, rol `admin` | `200`, respuesta paginada; filtros `rol` y `estado_validacion`, parámetros `page` (1+) y `limit` (1-100). |
| `PATCH /api/v1/admin/usuarios/{id}/validar` | JWT, rol `admin` | `200`; permite `Pendiente`, `Aprobado` o `Rechazado`; usuario inexistente: `404`. |
| `/api/v1/talento/perfil`, `/experiencia`, `/certificado-residencia` | JWT, rol `talento` | Consulta y actualización del perfil y experiencia; el certificado solo es descargable por su propietario o Admin. |
| `/api/v1/empresa/perfil` | JWT, rol `empresa` | Consulta/actualización de los datos de contacto de la empresa asociada. |
| `/api/v1/empresa/vitrina[/{id}]`, `/api/v1/empresa/solicitudes` | JWT, rol `empresa` | CV ciego de talentos aprobados y gestión de solicitudes por empresa. |
| `/api/v1/talento/solicitudes` | JWT, rol `talento` | Consulta de solicitudes propias. |
| `/api/v1/admin/talentos/pendientes`, `/api/v1/admin/solicitudes` | JWT, rol `admin` | Revisión de certificados y seguimiento de solicitudes OMIL. |

Ejemplo de registro de empresa, usando JSON:

```json
{
  "correo": "contacto@empresa.cl",
  "password": "Password123",
  "rut_empresa": "76543210-K",
  "nombre_empresa": "Empresa de ejemplo",
  "id_rubro": 1,
  "id_tipo_empresa": 1,
  "nombre_contacto": "Persona de contacto",
  "telefono_contacto": "+56 9 1234 5678"
}
```

El rubro, tipo de empresa, nombre y teléfono de contacto son obligatorios; no se asignan catálogos por defecto. El bootstrap inicial agrega catálogos con IDs fijos. Antes de migrar datos, revisar unicidad de correo/RUT y normalizar los correos a minúsculas; no convertir a minúsculas los hashes bcrypt ni contraseñas.

## Talento, vitrina y selección

- Una cuenta de talento pendiente puede completar nombre, RUT, teléfono y experiencia, y cargar un certificado de residencia PDF/Word/JPG/PNG de hasta 5 MB. Se almacena fuera de `wwwroot`; la aprobación se rechaza mientras falten el perfil o el certificado.
- La empresa se aprueba automáticamente al registrarse. Su perfil incluye rubro, tipo de empresa y contacto.
- El CV ciego expone un ID opaco y experiencias estructuradas (lugar de trabajo, cargo y fechas). No expone identidad ni campos de texto libre como resumen o descripción, porque podrían incluir datos identificatorios.
- Las solicitudes siguen provisionalmente los estados `Solicitado`, `Contactado`, `Entrevista`, `Seleccionado`, `No seleccionado` y `Cerrado`. Admin/OMIL administra las transiciones; empresa y talento consultan solo sus solicitudes.
- El esquema adicional está en [002-talent-selection.sql](../src/ProviEmplea.Web/Data/Schema/002-talent-selection.sql) y requiere aplicar primero [001-authentication.sql](../src/ProviEmplea.Web/Data/Schema/001-authentication.sql). Ambos son scripts bootstrap de ejecución única, no migraciones EF versionadas.

## Autenticación

- Los tokens nuevos usan HMAC-SHA256, issuer y audience requeridos, vencimiento configurable y claims de usuario/rol.
- En desarrollo el contrato OpenAPI JSON se publica en `/openapi/v1.json`; no se configuran formatters XML.
- Los IDs de rol se conservan: Admin `1`, Talento `2`, Empresa `3`. Los nombres de rol (`admin`, `talento`, `empresa`) se emiten como claims para autorización.
- El token legado no necesariamente contiene issuer/audience; no se acepta automáticamente. Consumidores y despliegue deben coordinar un cambio de token.
- El endpoint de API devuelve el bearer token en JSON para clientes REST autorizados. No almacenar tokens de navegador en `localStorage`/`sessionStorage`; la autenticación Razor deberá usar el mecanismo seguro descrito en el spec (cookie `HttpOnly`, `Secure`, `SameSite` y antifalsificación).
- Refresh/revocación y coexistencia durante la transición no están implementados; definirlos antes de aprobar el contrato externo para producción.
- Las contraseñas nuevas usan bcrypt con costo 10, compatible en formato con bcryptjs; debe comprobarse con hashes ficticios generados por el legado antes de habilitar migración de cuentas.

## Persistencia y despliegue

El modelo y los scripts bootstrap Oracle ([001-authentication.sql](../src/ProviEmplea.Web/Data/Schema/001-authentication.sql) y [002-talent-selection.sql](../src/ProviEmplea.Web/Data/Schema/002-talent-selection.sql)) cubren autenticación, perfiles y selección. Ambos son de ejecución única en un esquema vacío; todavía no reemplazan las migraciones EF Core versionadas exigidas por el spec. La identidad runtime no debe recibir permisos DDL. Las operaciones con datos requieren Oracle y parámetros JWT válidos al arrancar. dotConnect debe activarse por NuGet/Visual Studio o cargando `Devart:LicenseKey` desde configuración secreta.
