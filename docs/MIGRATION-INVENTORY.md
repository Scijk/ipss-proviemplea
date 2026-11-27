# Inventario del sistema legado

Fuente inspeccionada: `D:\Desarrollo\Repos\proviemplea-greudy`. Este documento registra la base observable para la migración; no implica que esas funciones estén implementadas en el destino.

> **Estado del destino actualizado:** las funciones implementadas y pendientes ya no se describen íntegramente en esta matriz de origen. Consultar [ESTADO-MIGRACION.md](ESTADO-MIGRACION.md) para el avance vigente del repositorio destino y usar las tablas siguientes como referencia de paridad del prototipo.

## Flujos prioritarios y trazabilidad

| Flujo del legado | Rutas/API de referencia | Destino requerido |
|---|---|---|
| Ingreso y validación de personas | `POST /api/v1/auth/login`, `POST /api/v1/auth/register/talento`, `GET /api/v1/admin/usuarios`, `PATCH /api/v1/admin/usuarios/{id}/validar`; interfaz `/login`, `/registro/talento`, `/talento/validacion`, `/admin/talentos` | Razor para acceso, registro y validación municipal; API JSON versionada; estados Pendiente/Aprobado/Rechazado y autorización por rol. |
| Perfil y antecedentes de talento | `/api/v1/talentos/perfil`, `/educacion`, `/laboral`, `/competencias`, `/idiomas`; `/api/v1/perfeccionamiento`; interfaz `/talento/perfil`, `/talento/completar-perfil`, `/talento/archivos` | Perfil, educación, experiencia, perfeccionamiento, competencias, idiomas, preferencias y archivos necesarios; operaciones restringidas al propietario y administración autorizada. |
| Empresas y usuarios asociados | `POST /api/v1/auth/register/empresa`; `/api/v1/empresas/perfil`, `/usuarios`, `/solicitudes`; interfaz `/registro/empresa`, `/empresa/perfil` | Registro y perfil de empresa, usuarios vinculados, aprobación/acceso municipal y operaciones limitadas a la empresa correspondiente. |
| Vitrina/CV ciego | `GET /api/v1/vitrina`, `GET /api/v1/vitrina/{id_talento}`; interfaz `/empresa/vitrina`, `/empresa/talento/{id}` | DTOs con lista permitida de campos; ocultar PII tanto en API como en Razor y probar explícitamente la ausencia de esos campos. |
| Solicitudes y seguimiento | `POST /api/v1/solicitudes`, `GET /api/v1/solicitudes/{id}`, `PATCH /api/v1/solicitudes/{id}/estado`, `PUT /api/v1/solicitudes/{id}/notas`; interfaz `/talento/solicitudes`, `/talento/historial`, `/empresa/seguimiento`, `/empresa/historial`, `/admin/solicitudes`, `/admin/seguimiento` | Flujo empresa-talento con estados Solicitud, Contactado, Entrevista, Seleccionado, No seleccionado y Cerrado; verificar permisos sobre cada registro y transición. |
| Administración prioritaria | `/api/v1/admin/usuarios`, `/talentos`, `/empresas`, `/solicitudes`; interfaz `/admin/talentos`, `/admin/empresas` y sus detalles | Validar personas, gestionar talentos/empresas y autorizar los flujos prioritarios. Otras capacidades se excluyen conforme al spec. |

El legado también define catálogos en `/api/v1/catalogos` para rubros, competencias, idiomas, rangos de renta y estados de seguimiento. Se deben trasladar únicamente los catálogos que requieran los formularios y operaciones en alcance.

## Datos de origen observados

Las migraciones Sequelize definen, entre otras, las tablas `roles`, `usuarios`, `talentos`, `empresas`, `usuarios_empresa`, `antecedentes_educacionales`, `antecedentes_laborales`, `perfeccionamiento`, `talento_competencia`, `talento_idioma`, `solicitudes_talento`, `archivos_talento` y catálogos asociados. El legado usa UUID para varias claves, borrado lógico con `fecha_eliminacion` y timestamps `fecha_creacion`/`fecha_actualizacion`. La migración Oracle debe fijar explícitamente el mapeo de identificadores, booleanos, fechas, texto largo, restricciones e índices.

Los roles existentes son Admin (1), Talento (2) y Empresa (3). Las contraseñas se guardan como hashes bcrypt; no migrar texto plano. Evaluar validación compatible y rehash progresivo o restablecimiento antes de cerrar el plan de autenticación.

## Verificaciones antes de portar

- La configuración Sequelize declara `dialect: 'mysql'`, pero hay consultas fuente con construcciones asociadas a otros motores (por ejemplo, `ILIKE` y `RETURNING`). No trasladarlas literalmente: contrastar el comportamiento con el esquema y respaldos de prueba.
- Verificar endpoints y validaciones contra consumidores React, OpenAPI y pruebas del legado; las rutas listadas no sustituyen esa matriz de paridad.
- El repositorio legado tenía cambios locales en `package-lock.json` y directorios no versionados al momento de inspección. No se modificaron.
- Mensajería, estadísticas/exportaciones generales y otros módulos quedan fuera de la primera entrega salvo dependencias funcionales aprobadas, según el spec.
