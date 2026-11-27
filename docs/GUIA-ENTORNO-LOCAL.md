# Guía de preparación del entorno local

Guía para el equipo que trabajará en la migración de ProviEmplea a .NET 10, ASP.NET Core Razor y Oracle 19c. El sistema legado React/Node.js/MySQL está en otro repositorio y no se necesita para ejecutar el proyecto destino.

> **Estado actual:** incluye autenticación y vistas Razor para completar el perfil mínimo de talento, mantener el perfil de empresa, revisar perfiles CV ciego y administrar solicitudes. Las páginas públicas se pueden previsualizar en `Development` sin Oracle; las áreas protegidas y las operaciones con datos requieren autenticación y Oracle/Devart con ambos scripts SQL aplicados. Todavía no hay `docker-compose.yml` ni migraciones EF Core versionadas. Consulta el [estado de migración](ESTADO-MIGRACION.md) para ver brechas y validaciones.

## 1. Instalar .NET 10 SDK

1. Descarga el **SDK de .NET 10 para Windows** desde [Descargas de .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0). Elige el instalador adecuado a tu equipo (normalmente x64). Instala la versión estable más reciente de la rama 10.0.
2. Cierra y vuelve a abrir PowerShell o Windows Terminal para que se actualice el `PATH`.
3. Comprueba que el SDK quedó disponible:

   ```powershell
   dotnet --list-sdks
   dotnet --info
   ```

   Debe aparecer una versión `10.0.xxx`. Si `dotnet` no se reconoce, reinicia la terminal y verifica la instalación del SDK antes de continuar.

No es necesario instalar el runtime de ASP.NET Core por separado para desarrollar: el SDK incluye lo necesario para compilar y ejecutar localmente.

## 2. Instalar un editor (opcional, recomendado)

- **Visual Studio:** instala una versión compatible con .NET 10 y selecciona la carga de trabajo **ASP.NET y desarrollo web**.
- También puedes usar otro editor con soporte para C# y Razor.

El editor no reemplaza el SDK; comprueba igualmente `dotnet --list-sdks`.

## 3. Previsualizar las vistas

Una vez instalado el SDK, puedes levantar la interfaz sin tener Oracle configurado:

```powershell
dotnet run --project src\ProviEmplea.Web
```

Abre la dirección mostrada por el proceso (por defecto `http://localhost:5000`) y revisa inicio, `/login`, `/registro/talento` y `/registro/empresa`. Estas páginas públicas se pueden cargar sin Oracle. Para crear cuentas, iniciar sesión y abrir las áreas protegidas se requiere Oracle configurado, Devart activado y los dos scripts de esquema aplicados.

Tras iniciar sesión, las páginas disponibles son `/talento/perfil`, `/talento/solicitudes`, `/empresa/perfil`, `/empresa/vitrina`, `/empresa/solicitudes`, `/admin/talentos` y `/admin/solicitudes`, según el rol. Las rutas protegidas no son una demostración independiente de datos: requieren usuarios y datos persistidos.

En `Development`, si falta una clave JWT se genera una nueva en memoria para permitir el arranque local. Cambia en cada ejecución y no es una configuración de producción.

## 4. Crear el esquema inicial de autenticación

Oracle y los paquetes/licencia Devart son necesarios para probar persistencia y operaciones de datos; no se requieren para previsualizar las páginas públicas en `Development`.

1. Construye/inicia Oracle 19c según la sección siguiente. Obtén el service name informado por la base; no asumas que será igual en todas las instalaciones.
2. Ejecuta `src\ProviEmplea.Web\Data\Schema\001-authentication.sql` una sola vez en un esquema Oracle vacío, con una cuenta autorizada para crear tablas. El script crea/siembra los roles y catálogos iniciales.
3. Después ejecuta `src\ProviEmplea.Web\Data\Schema\002-talent-selection.sql`, también una sola vez. Este script crea perfiles de talento, experiencia laboral, metadatos de certificados y solicitudes de selección, y sus estados iniciales. No borres ni vuelvas a ejecutar los scripts en un esquema con datos.
4. La identidad de ejecución de la aplicación no debe tener permisos DDL. Para el entorno local se puede usar el esquema de desarrollo aprobado por el equipo; para producción, separar el propietario de esquema de la cuenta runtime y otorgar solo los permisos DML necesarios.

Los certificados se almacenan por defecto en la carpeta `App_Data\Uploads` bajo el content root de la aplicación, fuera de `wwwroot` y excluida de Git. Para usar otra ubicación, configura `FileStorage:RootPath` en User Secrets; la identidad del proceso necesita permisos de lectura/escritura en esa carpeta. Los archivos admitidos actualmente son PDF, Word, JPG y PNG, hasta 5 MB.

Estos scripts son bootstrap provisionales y de ejecución única; no sustituyen las migraciones EF Core revisadas que se generarán y probarán más adelante.

## 5. Guardar configuración local fuera de Git

Desde PowerShell, en la raíz del repo, inicializa User Secrets y carga la cadena de conexión. Reemplaza usuario, contraseña, host, puerto y service name por los valores de tu Oracle local. Si la licencia Devart requiere una clave de conexión, consúltala con el administrador de licencias e inclúyela solo en User Secrets:

```powershell
dotnet user-secrets init --project src\ProviEmplea.Web
dotnet user-secrets set "ConnectionStrings:Oracle" "Server=localhost;Port=1521;User Id=USUARIO_LOCAL;Password=CONTRASENA_LOCAL;Service Name=SERVICE_NAME;Direct=True" --project src\ProviEmplea.Web
dotnet user-secrets set "Jwt:Issuer" "ProviEmplea.Local" --project src\ProviEmplea.Web
dotnet user-secrets set "Jwt:Audience" "ProviEmplea.Local.Api" --project src\ProviEmplea.Web
dotnet user-secrets set "Jwt:ExpiresMinutes" "60" --project src\ProviEmplea.Web
```

Activa dotConnect for Oracle usando el mecanismo autorizado del equipo (por ejemplo, **Tools > Oracle > Activate or Get License Information** en Visual Studio), o carga la clave en User Secrets para activación de runtime. Para no dejar la clave escrita en el historial:

```powershell
$license = Read-Host "Clave de activación Devart"
dotnet user-secrets set "Devart:LicenseKey" $license --project src\ProviEmplea.Web
Remove-Variable license
```

La herramienta de bootstrap lee los User Secrets del proyecto Web y usa la misma activación. No incluyas la clave de licencia en el connection string versionado ni la compartas por chat.

Genera una clave local aleatoria de al menos 32 bytes y guárdala en User Secrets:

```powershell
$bytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$rng.Dispose()
$key = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:SigningKey" $key --project src\ProviEmplea.Web
Remove-Variable key, bytes, rng
```

No uses valores de ejemplo en producción ni compartas la salida de `dotnet user-secrets list`; puede contener credenciales. Para otros integrantes, cada desarrollador configura sus propios secretos locales.

## 6. Crear el administrador inicial

El esquema no inserta una cuenta Admin ni incluye credenciales por defecto. Después de guardar la conexión Oracle en User Secrets, ejecuta la herramienta de bootstrap. Solicitará el correo y la contraseña en la terminal, sin mostrar la contraseña:

```powershell
dotnet run --project tools\ProviEmplea.AdminBootstrap
```

La contraseña inicial debe tener entre 12 y 128 caracteres e incluir mayúscula, minúscula, número y símbolo. La herramienta solo crea la primera cuenta Admin activa y se niega a crear otra si ya existe una.

## 7. Restaurar, compilar y ejecutar el proyecto

Abre PowerShell en la carpeta raíz del repositorio destino:

```powershell
Set-Location 'D:\Desarrollo\Repos\ipss-proviemplea'
dotnet restore ProviEmplea.slnx
dotnet build ProviEmplea.slnx
dotnet run --project src\ProviEmplea.Web
```

La restauración requiere acceso al paquete NuGet Devart y a la activación/licencia de dotConnect acordada por el equipo. Si el proveedor pide una clave, usa el mecanismo seguro aprobado; no agregues claves al proyecto ni a Git.

En la salida de `dotnet run`, busca la dirección indicada por ASP.NET Core, por ejemplo `http://localhost:5000` o `https://localhost:7xxx`, y ábrela en el navegador. La página debe mostrar el inicio de ProviEmplea.

En otra ventana de PowerShell, consulta el endpoint de salud usando la dirección HTTP que mostró la aplicación:

```powershell
Invoke-RestMethod 'http://localhost:5000/health/live'
```

Si la aplicación mostró un puerto HTTP distinto, reemplaza `5000`. La respuesta esperada es un objeto JSON con `status` igual a `ok`. Para detener el servidor, vuelve a su terminal y presiona `Ctrl+C`.

El endpoint `GET /health/live` verifica que el proceso responde. `GET /health/ready` intenta conectar con Oracle y solo devuelve `200` si la base y la activación Devart están disponibles; devuelve `503` si no puede conectar o falta la activación. Para las operaciones de datos, el esquema debe tener aplicados `001-authentication.sql` y `002-talent-selection.sql`.

Si se usa la dirección HTTPS y el navegador advierte sobre el certificado de desarrollo, se puede confiar el certificado local con:

```powershell
dotnet dev-certs https --trust
```

Confirma la solicitud de Windows y reinicia la aplicación. Este certificado es solo para desarrollo local.

## 8. Instalar Docker Desktop y habilitar Linux containers

Docker se usará para Oracle local. La imagen de Oracle descrita por el spec se construye localmente con medios de instalación autorizados; no basta con instalar Docker.

1. Revisa los [requisitos de Docker Desktop para Windows](https://docs.docker.com/desktop/setup/install/windows-install/).
2. Instala Docker Desktop usando el backend **WSL 2** recomendado y selecciona contenedores Linux.
3. Si WSL 2 no está instalado, abre PowerShell como administrador y ejecuta:

   ```powershell
   wsl --install
   ```

   Reinicia Windows si se solicita. Luego revisa el estado:

   ```powershell
   wsl --status
   wsl --version
   ```

4. Abre Docker Desktop, espera a que indique que el motor está iniciado y verifica desde una nueva terminal:

   ```powershell
   docker version
   docker compose version
   docker run --rm hello-world
   ```

   El último comando debe terminar mostrando el mensaje de bienvenida de Docker. Si hay un error de permisos o el motor no está disponible, confirma que Docker Desktop esté iniciado y consulta la guía de instalación de Docker enlazada arriba.

## 9. Oracle 19c local: preparación cuando estén disponibles los medios

**No descargues ni compartas binarios de Oracle hasta confirmar con el responsable del equipo la edición y los derechos de uso aplicables.** Oracle Database 19c en el flujo especificado requiere construir una imagen a partir de los binarios oficiales; no se debe asumir que existe una imagen pública lista para usar.

Cuando el equipo haya aprobado edición, licencia y Release Update (RU):

1. Obtén los binarios de instalación de Oracle 19c desde los canales autorizados de Oracle y verifica que correspondan a la edición/RU acordada.
2. Sigue las instrucciones vigentes del repositorio oficial [Oracle Database Docker images](https://github.com/oracle/docker-images/tree/main/OracleDatabase/SingleInstance) para preparar los archivos y construir la imagen 19c.
3. Usa contenedores Linux en Docker Desktop. Considera que la inicialización de la base puede tardar varios minutos y requerir recursos relevantes de memoria y disco.
4. Mantén los ZIP de instalación, imágenes, volúmenes de datos, respaldos y contraseñas fuera de Git y de carpetas compartidas no autorizadas.
5. Crea para la aplicación un usuario/esquema dedicado con permisos mínimos. No configures la aplicación para conectarse como `SYS` o `SYSTEM`.

El repositorio destino aún no trae Docker Compose ni healthcheck para Oracle. Hasta agregarlos, no hay un comando `docker compose up` específico del proyecto.

## 10. Devart y paquetes NuGet

El proyecto fija `Devart.Data.Oracle.EFCore` 11.2.237.10, cuyo paquete NuGet declara dependencia de EF Core 10.0.x. Antes de usarlo, el equipo debe confirmar licenciamiento/activación y compatibilidad con el RU de Oracle institucional. Consulta [dotConnect for Oracle](https://www.devart.com/dotconnect/oracle/), su [historial de versiones](https://www.devart.com/dotconnect/oracle/revision_history.html) y las instrucciones oficiales de [activación](https://docs.devart.com/dotconnect/oracle/Activation.html).

No incluyas claves Devart, contraseñas ni cadenas de conexión reales en Git. Mantén la configuración local en User Secrets.

## 11. Herramientas que no hace falta instalar globalmente

- **Node.js, npm, React y MySQL:** pertenecen al sistema legado. No son requisitos para compilar ni ejecutar el destino .NET.
- **Bootstrap 5.2 y jQuery 3.6:** se incorporarán como dependencias de la aplicación cuando se construyan las páginas; no se instalan globalmente en Windows.
- **IIS y Web Deploy:** se necesitan para preparar/publicar en el entorno institucional, no para ejecutar localmente.
- **Oracle Client:** dotConnect ofrece modo de conexión directa; la necesidad concreta se decidirá al configurar el proveedor y la conexión.

## 12. Problemas frecuentes

| Síntoma | Qué revisar |
|---|---|
| `dotnet` no se reconoce | Instalar el SDK (no solo el runtime), abrir una terminal nueva y ejecutar `dotnet --info`. |
| Error al restaurar o compilar | Confirmar que se está en la raíz correcta del repo, que el SDK 10 aparece en `dotnet --list-sdks` y guardar el mensaje completo para el equipo. |
| El puerto está ocupado | Usar la dirección/puerto que indique `dotnet run`; detener otro proceso de desarrollo si corresponde. |
| Docker no conecta con el engine | Abrir Docker Desktop, esperar que termine de iniciar y revisar `wsl --status` y `docker version`. |
| Docker intenta usar Windows containers | Cambiar Docker Desktop a Linux containers; Oracle 19c en esta guía se ejecuta en contenedor Linux. |
| No se encuentra una imagen Oracle 19c pública | Es esperado: se debe construir conforme a las instrucciones oficiales y con los binarios autorizados de Oracle. |
| Falta `ConnectionStrings:Oracle` | En Development la interfaz aún puede verse, pero `GET /health/ready` y las operaciones con datos no funcionarán. Configura User Secrets para probarlas. En otros ambientes la conexión es obligatoria. |
| Falta configuración JWT | En Development se genera una clave efímera para arrancar la app, pero las sesiones no persisten entre reinicios. Configura una clave local persistente para probar login; en otros ambientes issuer, audience y clave son obligatorios. |
| `GET /health/ready` devuelve `503` | Revisar que Oracle esté iniciado y que el service name/usuario/contraseña sean correctos; comprobar que Devart esté activado. Para las operaciones de datos, verificar además que ambos scripts de esquema estén aplicados en orden. |

## Enlaces oficiales

- [.NET 10: SDK y descargas](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Instalación de Docker Desktop en Windows](https://docs.docker.com/desktop/setup/install/windows-install/)
- [Imágenes de Oracle Database para Docker](https://github.com/oracle/docker-images/tree/main/OracleDatabase/SingleInstance)
- [dotConnect for Oracle](https://www.devart.com/dotconnect/oracle/)
- [Spec de migración](../MIGRATION-SPEC.md)
- [Inventario del sistema legado](MIGRATION-INVENTORY.md)
