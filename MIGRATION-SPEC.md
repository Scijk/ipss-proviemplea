# Especificación de migración de ProviEmplea

## Objetivo

Migrar ProviEmplea desde el prototipo React + Node.js/Express + MySQL a una solución institucional basada en C#, ASP.NET Core 10 LTS, Razor, Bootstrap 5.2, jQuery 3.6 y Oracle 19c con el proveedor Devart para Entity Framework Core. La solución debe exponer API REST en JSON, usar JWT para autenticación y desplegarse en IIS mediante Web Deploy.

El repositorio de destino ya contiene una primera vertical de autenticación, registro y validación administrativa en ASP.NET Core 10. Esta especificación define el alcance completo pendiente; el estado observable, las brechas y la propuesta de resolución se detallan en [el análisis de la solución propuesta](docs/ANALISIS-SOLUCION-PROPUESTA.md). El prototipo se usa como referencia funcional, no se presume código reutilizable de su stack.

## Decisiones de alcance y plataforma

- **Alcance funcional prioritario:** ingreso de personas (registro, inicio de sesión y flujo de validación municipal), módulo de empleados/talentos y módulo de empresas. Preservar el CV ciego, permisos por rol y flujos de solicitud/seguimiento que conectan empresas con talentos.
- **Fuera del alcance inicial salvo dependencia funcional:** mensajería, dashboard/estadísticas/exportaciones generales y otros módulos OMIL no necesarios para los tres módulos prioritarios. La administración requerida para validar personas y gestionar empleados/empresas sí está incluida.
- **Persistencia local:** Oracle Database 19c en Docker para desarrollo y pruebas locales; no se asume que Docker sea la plataforma Oracle de producción.
- **Origen de migración:** MySQL. La mención de PostgreSQL en documentación del prototipo se trata como residual: el backend configura Sequelize con dialecto `mysql` y usa `mysql2`.
- **Autenticación e integración:** JWT para autenticación/autorización y API REST solo en JSON. XML queda fuera del alcance.

## Decisión de inicio

**La migración se considera apta para iniciar** con .NET 10 LTS y el stack municipal solicitado, bajo el supuesto de planificación acordado de que “IIS 7 o superior” significa que la infraestructura institucional disponible para este proyecto es IIS 10 sobre Windows Server compatible con ASP.NET Core 10. IIS 7 es el mínimo legado del requisito, no la versión que se usará para producción.

Este supuesto elimina el bloqueo para iniciar análisis, diseño, desarrollo y pruebas. La confirmación del servidor concreto y del Hosting Bundle/ASP.NET Core Module debe completarse antes de preparar el despliegue en IIS. Oracle 19c local en Docker permite iniciar mientras se habilitan la instancia institucional y la licencia/paquetes de Devart. Si la infraestructura real difiere del supuesto, se registra como riesgo de despliegue y se acuerda su resolución con el municipio.

## Compatibilidad técnica verificada

Evaluación realizada el **13 de noviembre de 2026** a partir de la documentación oficial vinculada en [Fuentes de compatibilidad](#fuentes-de-compatibilidad).

| Requisito | Evaluación | Evidencia y decisión |
|---|---|---|
| C#, ASP.NET Core 10+, Razor | **Compatible.** | ASP.NET Core 10 incluye Razor Pages y es una opción soportada para aplicaciones web renderizadas en servidor. En documentación y proyecto, usar el nombre actual **.NET 10 / ASP.NET Core 10**, no “.NET Core 10”. |
| “.NET 10 o superior” | **Compatible; preferir .NET 10 LTS.** | Al momento de esta revisión .NET 11 está disponible y es STS, mientras que .NET 10 es LTS; ambos tienen fin de soporte publicado para el 14 de noviembre de 2028. Para un proyecto municipal nuevo se recomienda fijar `net10.0` y la última actualización de servicio compatible, en vez de adoptar .NET 11 recién publicado sin una necesidad concreta. |
| Bootstrap 5.2 + jQuery 3.6 | **Compatible.** | Bootstrap 5 no requiere jQuery y sus componentes JavaScript usan APIs nativas; jQuery 3.6 puede convivir para las interacciones que lo necesiten. No inicializar los mismos componentes/elementos con dos controladores JavaScript distintos. |
| Oracle 19c + Devart EF Core | **Compatible con versiones fijadas y validación del motor real.** | Devart declara soporte para .NET y EF Core 10 y compatibilidad con Oracle Database 8–26ai. El proveedor permite EF Core sobre Oracle y ofrece acceso a funciones Oracle/PL/SQL. EF Core no sustituye PL/SQL: usarlo para CRUD y consultas mapeadas, y llamadas explícitas a procedimientos/funciones PL/SQL donde el diseño lo requiera. Validar el RU exacto de Oracle 19c, la versión del proveedor EF Core 10 y el licenciamiento Devart. |
| Oracle 19c en Docker local | **Viable; requiere construir la imagen con medios de Oracle.** | El repositorio oficial de Oracle proporciona Dockerfiles y scripts para Oracle Database 19c (19.3.0) Enterprise Edition y Standard Edition 2, pero exige aportar los binarios de instalación de Oracle. La imagen local debe construirse de acuerdo con los términos de uso aplicables; no asumir que es una imagen pública lista para descargar ni que una edición tiene licencia gratuita. |
| IIS 7 o superior + ASP.NET Core 10 | **Compatible bajo el supuesto acordado de IIS 10.** | “IIS 7 o superior” por sí solo no garantiza compatibilidad: también puede referirse a IIS 7/7.5 sobre Windows Server 2008/2008 R2, que no cumple el requisito de alojamiento indicado por Microsoft para ASP.NET Core actual. Para esta migración se asume IIS 10 y Windows Server compatible; Web Deploy puede publicar la aplicación, pero no instala ni reemplaza el runtime y ASP.NET Core Module. |
| Web Deploy | **Compatible como mecanismo de publicación, sujeto a IIS/OS compatible.** | Web Deploy es una herramienta para sincronizar/publicar contenido y configuración a IIS. No solventa una versión de Windows/IIS fuera de los requisitos de alojamiento de ASP.NET Core. |
| API REST JSON | **Compatible.** | ASP.NET Core Web API usa JSON de forma predeterminada. Se documentarán y probarán exclusivamente contratos JSON; no se configurarán formatters XML. |

### Configuración objetivo recomendada

- **Aplicación:** `net10.0`, ASP.NET Core 10, Razor Pages; actualizar al último parche 10.0 soportado.
- **Servidor:** Windows Server 2022 o 2025 con IIS 10, Hosting Bundle de .NET 10 y ASP.NET Core Module. Esto supera el mínimo documentado de Windows Server 2012 R2 y evita depender de sistemas operativos antiguos o fuera de soporte.
- **Publicación:** paquete Web Deploy compatible con el entorno institucional; conservar el `web.config` generado por `dotnet publish` y verificar el método de publicación en preproducción.
- **Datos:** Oracle Database 19c local en Docker y versión de dotConnect for Oracle/Devart que soporte .NET 10 y EF Core 10; probar migraciones, PL/SQL requerido y conectividad contra el RU institucional de Oracle 19c.
- **Interfaz:** Bootstrap 5.2 y jQuery 3.6 pueden incluirse en Razor; Bootstrap JS no necesita jQuery.
- **API:** JSON como único formato REST acordado.

### Oracle 19c local con Docker

- Usar como referencia los Dockerfiles y scripts oficiales de Oracle para Oracle Database 19c (19.3.0); construir la imagen local a partir de los binarios oficiales de la edición autorizada.
- Ejecutar la imagen como contenedor Linux; en estaciones Windows, configurar Docker Desktop/WSL 2 para Linux containers.
- No guardar los ZIP de instalación, imágenes generadas, contraseñas ni secretos en Git. Añadir a `.gitignore` el archivo local de variables y cualquier ruta de binarios/artefactos que corresponda.
- Definir con Docker Compose el puerto Oracle, volumen persistente para datos, variables requeridas y healthcheck; documentar el tiempo inicial de creación y los comandos para levantar/detener el servicio.
- Crear un usuario/esquema de aplicación con permisos mínimos; no usar `SYS` o `SYSTEM` en la conexión de la aplicación.
- Mantener persistencia del volumen entre reinicios y documentar cómo recrear una base de desarrollo sin afectar datos del host.
- Confirmar licencia y derecho de uso de la edición/binarios en el contexto de desarrollo del equipo. No tomar una instancia local Docker como equivalente a producción.

### Contingencia si la infraestructura real es IIS 7/7.5

No hay un ajuste de Web Deploy ni una publicación self-contained que convierta de forma soportada Windows Server 2008/2008 R2 con IIS 7/7.5 en plataforma para ASP.NET Core 10. Si la verificación previa a despliegue contradice el supuesto acordado, la opción que conserva **todas** las tecnologías solicitadas es actualizar la plataforma a Windows Server 2022/2025 + IIS 10. Si eso no es posible, el municipio debe cambiar formalmente el requisito de servidor/runtime; una aplicación ASP.NET Framework sería otra arquitectura, no una migración equivalente.

## Arquitectura objetivo

- **Aplicación web:** ASP.NET Core 10 con Razor Pages para interfaz renderizada en el servidor. Mantener separación entre páginas/modelos de página, servicios de aplicación y acceso a datos. Usar controladores API para los consumidores REST, sin duplicar la lógica de negocio.
- **Interfaz:** Bootstrap 5.2 y jQuery 3.6 para los flujos interactivos. Organizar páginas y navegación por los tres roles: Talento, Empresa y Admin/OMIL.
- **Persistencia:** Oracle Database, acceso mediante EF Core y el proveedor Devart. Usar migraciones versionadas de esquema compatibles con Devart; reservar PL/SQL para objetos/rutinas que requiera la integración o el diseño aprobado de Oracle.
- **Integración:** API REST versionada con contratos explícitos solo en JSON. Mantener páginas Razor y endpoints API sobre servicios compartidos.
- **Despliegue:** IIS y Web Deploy, con configuración separada por ambiente y secretos fuera del repositorio.

## Alcance funcional que debe preservarse

1. **Ingreso de personas:** registro de personas/talentos, inicio de sesión, manejo de credenciales, estados de validación y aprobación municipal. Roles existentes: Admin (ID 1), Talento (ID 2) y Empresa (ID 3).
2. **Empleados/talentos:** perfil laboral, educación, perfeccionamiento, competencias, idiomas y preferencias de empleo; incluir gestión de CV/documentos si es necesaria para completar o validar el perfil.
3. **Empresas:** perfil y usuarios asociados, datos requeridos para aprobación municipal y administración de acceso.
4. **Flujos compartidos:** vitrina/CV ciego con controles de no exposición de PII, catálogos necesarios para los perfiles, y solicitudes/estados/seguimiento de contacto entre empresa y talento.
5. **Administración mínima necesaria:** vistas y operaciones para validar personas, gestionar empleados y empresas, y autorizar los flujos anteriores. Otras funciones administrativas quedan fuera del primer alcance salvo confirmación.

## Pasos de implementación

### 1. Confirmar requisitos y preparar el repositorio

- Confirmar con el municipio las versiones exactas de .NET, Oracle 19c, Devart EF Core, IIS y Web Deploy que admite el entorno de producción.
- Tomar como base de planificación el supuesto acordado: IIS 10 sobre Windows Server compatible con ASP.NET Core 10. Verificar la versión concreta de Windows Server, IIS, Hosting Bundle y ASP.NET Core Module antes del despliegue; esta verificación no bloquea el inicio del desarrollo.
- Definir consumidores y contrato de la API JSON, incluyendo claims/roles JWT y requisitos de autenticación.
- Establecer la estructura de solución, configuración por ambiente y estrategia de ramas/CI del repositorio.
- Preparar Docker Compose local para Oracle 19c cuando se disponga de los binarios y la edición autorizada.

**Salida:** requisitos de plataforma y decisiones de interfaz/API aprobados.

### 2. Inventariar el prototipo y fijar contratos

- Usar el código actual como referencia para los módulos de ingreso de personas, talentos/empleados y empresas: rutas, validaciones, permisos, respuestas, estados y comportamientos.
- Documentar el contrato REST JSON actual de esos módulos y decidir qué endpoints deben mantenerse, versionarse o retirarse. Actualizar conjuntamente documentación OpenAPI y consumidores.
- Crear una matriz de paridad funcional de esos módulos que vincule cada flujo actual a una página Razor, servicio y endpoint del destino.
- Revisar el estado de cada módulo del prototipo y etiquetar capacidades incompletas para no presentarlas como funcionalidad lista para producción.

**Salida:** inventario de funcionalidades, contratos y matriz de trazabilidad.

### 3. Diseñar Oracle y la migración de datos

- Traducir las entidades y relaciones de alcance a un modelo Oracle normalizado: usuarios, roles, talentos, empresas y sus usuarios, educación, experiencia laboral, perfeccionamiento, competencias, idiomas, solicitudes/estados, archivos necesarios y catálogos.
- Definir claves, restricciones únicas, claves foráneas, índices, secuencias/identidades, auditoría y política de borrado/retención.
- Resolver explícitamente las diferencias de tipos: UUID, booleanos, fechas y zonas horarias, textos largos, paginación, búsquedas sin distinguir mayúsculas y comparación/validación de identificadores locales.
- Implementar el modelo EF Core con la versión compatible de Devart y generar migraciones revisables. Añadir scripts PL/SQL versionados solo para objetos que EF Core no represente o que estén aprobados como parte de la arquitectura.
- Diseñar una carga repetible desde MySQL, con mapeo de identificadores, deduplicación, registro de errores y reconciliación de conteos/relaciones. No copiar datos productivos o personales a ambientes no autorizados.
- Decidir cómo migrar contraseñas bcrypt existentes. Verificar compatibilidad de los hashes y planificar su validación/rehash al iniciar sesión o un restablecimiento de contraseña; no guardar ni migrar contraseñas en texto plano.
- Mantener archivos fuera de la base de datos salvo decisión institucional explícita; trasladar archivos a almacenamiento aprobado y validar permisos, tipos, límites, metadatos y correspondencia con registros.

**Salida:** modelo Oracle aprobado, migraciones de esquema y procedimiento de carga probado con datos de prueba.

### 4. Construir el núcleo de aplicación

- Crear configuración de ASP.NET Core, manejo centralizado de errores, logging y health checks.
- Implementar repositorios/acceso EF Core y servicios de aplicación para reglas de negocio compartidas por Razor y REST.
- Implementar autenticación y autorización por rol en el servidor. Proteger formularios Razor frente a CSRF; aplicar autorización en cada operación del API, no solo en la interfaz.
- Emitir y validar JWT para autenticación y autorización. Definir expiración, issuer, audience, claims/roles, rotación y almacenamiento seguro de la clave de firma. Para solicitudes del navegador, mantener el token fuera de almacenamiento JavaScript accesible (por ejemplo, cookie `HttpOnly`, `Secure`, `SameSite` más protección antifalsificación); los consumidores REST externos enviarán bearer token. Definir estrategia de refresh/revocación antes de cerrar el contrato.
- Aplicar validación de entrada, límites de carga, rate limiting donde aplique y respuestas de error coherentes.

**Salida:** flujo de autenticación funcional, autorización verificada en servidor y acceso Oracle operativo.

### 5. Implementar la API REST

- Exponer endpoints de ingreso/autenticación, catálogos requeridos, talentos/empleados, empresas, vitrina/CV ciego y solicitudes/seguimiento. Incluir operaciones administrativas o de archivos solo cuando sean necesarias para esos flujos prioritarios.
- Versionar rutas y publicar esquemas de request/response, códigos HTTP, paginación y errores en OpenAPI.
- Usar DTOs dedicados y evitar serializar entidades EF directamente. Aplicar listas permitidas de campos y filtros.
- Diseñar las respuestas de vitrina para que no incluyan PII; probar la ausencia de esos campos con pruebas automatizadas.
- Exponer y documentar solo JSON; validar el `Content-Type` de entrada y responder con contratos JSON consistentes.

**Salida:** contrato REST documentado y pruebas de autorización y serialización por módulo.

### 6. Migrar la interfaz a Razor

- Crear layout compartido, navegación y componentes de formulario compatibles con Bootstrap 5.2.
- Migrar los flujos públicos y de acceso; luego las páginas de Talento, Empresa y Admin/OMIL, vinculándolas a servicios de aplicación.
- Mantener búsqueda, filtros, paginación, estados vacíos, errores y confirmaciones de operación.
- Rehacer interacciones con jQuery 3.6 únicamente donde sean necesarias; no incorporar React como dependencia del nuevo frontend.
- Conservar el flujo de CV ciego en interfaz y servidor, además de los requisitos de accesibilidad y diseño institucional definidos por el municipio.
- Configurar la URL/base de API y otros parámetros por ambiente, sin hardcodear hosts de producción.

**Salida:** paridad funcional revisada por rol en una versión desplegable.

### 7. Validar funcionalidad, seguridad y datos

- Pruebas unitarias para servicios, validaciones, estados de solicitudes y reglas de autorización.
- Pruebas de integración contra Oracle de prueba para migraciones, restricciones y consultas representativas.
- Pruebas de API para contratos JSON, errores, paginación, JWT y permisos.
- Pruebas de extremo a extremo de registro/aprobación, perfil de empleado/talento, administración de empresa, vitrina CV ciego, solicitud/seguimiento y carga de archivos incluida en el alcance.
- Ensayar conversión y carga de datos con conteos de filas, claves huérfanas, duplicados, archivos faltantes y muestreo funcional; documentar reconciliación y rollback.
- Revisar el aislamiento de datos por rol, exposición de PII, seguridad de archivos, CSRF, XSS, SQL/inyección, secretos y configuración de JWT/cookies.

**Salida:** resultados de pruebas aceptados y reconciliación de migración firmada.

### 8. Preparar e implementar despliegue institucional

- Preparar publicación para IIS con el Hosting Bundle/runtime requerido y configuración de Application Pool adecuada.
- Crear paquete y parámetros de Web Deploy por ambiente. No incluir secretos ni cadenas de conexión reales en el paquete o el repositorio.
- Configurar conexión Oracle, certificados, permisos de carpetas de archivos/logs, HTTPS, límites de carga, logs y monitoreo.
- Ensayar publicación, actualización de esquema y rollback en un entorno de preproducción con IIS/Oracle equivalentes a producción.
- Acordar ventana de corte, respaldo verificable, carga final, comprobación de salud y plan de reversa antes de retirar el sistema anterior.

**Salida:** despliegue reproducible en IIS y procedimiento operativo validado.

## Criterios de aceptación

- La solución compila y publica con la versión de .NET y proveedor Devart aprobados, y conecta a Oracle desde los ambientes autorizados.
- Todas las funciones de ingreso de personas, empleados/talentos y empresas incluidas en la matriz de paridad tienen una ruta de usuario verificada por rol o se encuentran expresamente fuera del alcance aprobado.
- Las migraciones Oracle son repetibles en una base vacía y la carga desde el origen se reconcilia sin relaciones huérfanas ni pérdida de archivos aprobados.
- La vitrina y sus endpoints no revelan información personal que deba permanecer oculta en el flujo CV ciego.
- Las páginas Razor y endpoints REST aplican autenticación, autorización, validación y manejo de errores en el servidor.
- Los contratos API JSON y consumidores acordados funcionan con JWT y autorización por rol.
- El paquete Web Deploy se instala y actualiza en el IIS institucional de preproducción, con configuración y rollback documentados.

## Riesgos y decisiones pendientes

| Tema | Riesgo / decisión necesaria |
|---|---|
| Compatibilidad de plataforma | Confirmar Windows Server, IIS, Hosting Bundle de .NET 10, versión de Oracle y Devart compatibles entre sí. |
| Oracle local | La imagen oficial de Oracle Database 19c requiere binarios de instalación aportados por el equipo; confirmar disponibilidad, edición y derechos de uso antes de automatizar Docker Compose. |
| Oracle institucional | Confirmar Release Update (RU), service name, conectividad y configuración de caracteres de la instancia municipal Oracle 19c. |
| Origen de datos | MySQL es el origen asumido; verificar dump/esquema de migración y respaldos disponibles al comenzar la carga. |
| Contraseñas | Los hashes bcrypt pueden requerir un verificador compatible y rehash progresivo o restablecimiento de credenciales. |
| JWT | Definir expiración, refresh/revocación y estrategia segura de entrega/almacenamiento de tokens para Razor y consumidores externos. |
| Archivos y privacidad | Confirmar almacenamiento institucional, retención y permisos; asegurar la no exposición de PII en CV ciego. |
| Paridad | Distinguir funciones terminadas de incompletas en los módulos priorizados y validar si mensajería/estadísticas/exportaciones quedan fuera de la primera entrega. |

## Fuentes de compatibilidad

- Microsoft, [Host ASP.NET Core en Windows con IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0): sistemas operativos compatibles para alojamiento IIS, instalación del Hosting Bundle y comportamiento del ASP.NET Core Module.
- Microsoft, [Hosting Bundle](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-10.0): runtime y módulo requeridos para ejecutar aplicaciones ASP.NET Core en IIS.
- Microsoft, [Publicar una aplicación ASP.NET Core en IIS](https://learn.microsoft.com/en-us/aspnet/core/tutorials/publish-to-iis?view=aspnetcore-10.0): publicación, Hosting Bundle, `web.config` y despliegue en IIS.
- Microsoft, [Instalar .NET en Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows): versiones de Windows compatibles con .NET 10/11 y requisitos de SDK.
- Microsoft, [Política de soporte .NET](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core): clasificación LTS/STS y fechas de soporte de .NET 10 y .NET 11.
- Devart, [dotConnect for Oracle](https://www.devart.com/dotconnect/oracle/): plataformas .NET, rangos EF Core y Oracle Database, soporte ORM y capacidades Oracle/PL/SQL.
- Devart, [historial de versiones de dotConnect for Oracle](https://www.devart.com/dotconnect/oracle/revision_history.html): soporte de EF Core 10 y 11 y niveles de actualización del proveedor.
- Bootstrap, [JavaScript en Bootstrap 5.2](https://getbootstrap.com/docs/5.2/getting-started/javascript/): funcionamiento JavaScript y dependencias de componentes.
- Bootstrap, [introducción 5.2](https://getbootstrap.com/docs/5.2/getting-started/introduction/): versión 5.2.3 y formas de incluir CSS/JavaScript.
- Microsoft, [Formato de respuesta de ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-10.0): JSON predeterminado y opciones de negociación de contenido.
- Microsoft, [Introducción a Web Deploy](https://learn.microsoft.com/en-us/iis/publish/using-web-deploy/introduction-to-web-deploy): alcance de Web Deploy para publicación/sincronización con IIS, incluidos ejemplos de IIS 7/7.5.
- Oracle, [Oracle Database container images](https://github.com/oracle/docker-images/tree/main/OracleDatabase/SingleInstance): versiones/ediciones 19c disponibles, proceso de construcción de la imagen y requisito de aportar los binarios de instalación.
