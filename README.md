# ProviEmplea

Migración de ProviEmplea a ASP.NET Core 10, Razor y Oracle 19c según [MIGRATION-SPEC.md](MIGRATION-SPEC.md). El análisis del estado actual, las brechas y la ruta propuesta está en [docs/ANALISIS-SOLUCION-PROPUESTA.md](docs/ANALISIS-SOLUCION-PROPUESTA.md). El sistema legado de referencia está en `D:\Desarrollo\Repos\proviemplea-greudy`; el inventario de sus módulos y contratos está en [docs/MIGRATION-INVENTORY.md](docs/MIGRATION-INVENTORY.md).

El estado vigente de funcionalidades, brechas, decisiones provisionales y validaciones está en [docs/ESTADO-MIGRACION.md](docs/ESTADO-MIGRACION.md). Los cambios relevantes se registran en [CHANGELOG.md](CHANGELOG.md).

## Requisitos de desarrollo

- .NET 10 SDK.
- Docker Desktop con contenedores Linux para Oracle 19c local, una vez disponibles los binarios oficiales y confirmados los derechos de uso.
- Acceso aprobado al paquete/licencia de Devart y a la instancia Oracle antes de habilitar persistencia.

## Revisar las vistas

En `Development`, las páginas públicas se pueden revisar sin Oracle ni secretos persistentes:

```powershell
dotnet restore ProviEmplea.slnx
dotnet build ProviEmplea.slnx
dotnet run --project src\ProviEmplea.Web
```

Abre `http://localhost:5000`, `/login`, `/registro/talento` o `/registro/empresa`. El inicio de sesión, registro y operaciones de perfil/selección requieren Oracle configurado, activación Devart y ambos scripts de esquema aplicados. En producción, conexión Oracle y configuración JWT son obligatorias.

La vertical funcional actual incluye registro e inicio de sesión JSON/Razor, JWT en cookie HttpOnly para navegador, completar perfil de talento y cargar certificado de residencia, perfil de empresa, vitrina de CV ciego, solicitudes de selección y revisión/seguimiento Admin. Las vistas protegidas requieren iniciar sesión con el rol correspondiente. La matriz de paridad funcional aún está incompleta. El perfil de lanzamiento local activa Development; si falta una clave JWT, se genera una efímera al iniciar y no sirve para producción.

## Migración de datos

Los scripts `001-authentication.sql` y `002-talent-selection.sql`, en `src/ProviEmplea.Web/Data/Schema`, deben ejecutarse una sola vez y en ese orden, en una base vacía, con credenciales de despliegue autorizadas. Son bootstrap SQL, no migraciones EF; no deben ejecutarse con la identidad runtime. Las siguientes versiones deben pasar a migraciones EF Core revisadas una vez que el equipo pueda restaurar/probar el proveedor Devart. El origen usa Sequelize configurado con dialecto MySQL; la migración de datos requiere validar el esquema y un respaldo de prueba autorizado. No copiar datos personales a ambientes no aprobados. Los certificados se guardan por defecto fuera de `wwwroot`, en `App_Data/Uploads` bajo el content root, ruta excluida de Git.

La solución compila con .NET SDK 10.0.404. La conexión y las operaciones contra Oracle aún deben validarse con la base local y la activación Devart del equipo.
