# Análisis de la solución propuesta para ProviEmplea

**Fecha de corte:** 21 de noviembre de 2026  
**Repositorios revisados:** prototipo `proviemplea-greudy` y destino `ipss-proviemplea`  
**Propósito:** apoyar la decisión sobre qué conservar como referencia, qué corregir y qué construir para completar la migración institucional.

> **Actualización:** este análisis conserva evidencia y conclusiones de la revisión inicial del 21 de noviembre de 2026. La implementación del destino avanzó después de esa revisión; no usar las frases históricas de “no implementado” como estado actual. Consultar [ESTADO-MIGRACION.md](ESTADO-MIGRACION.md) para el estado vigente al 26 de noviembre de 2026.

## Resumen ejecutivo

La migración es **viable y puede continuar**, pero debe tratarse como una reconstrucción por módulos con validación de reglas de negocio, no como una conversión mecánica del código o del esquema. El prototipo demuestra flujos y una primera experiencia de usuario; la evidencia revisada no permite considerarlo un producto listo para producción ni una especificación completa del comportamiento municipal.

En la fecha de esta revisión inicial, el destino solo contenía una primera vertical ASP.NET Core 10 de acceso, registro y validación. Desde entonces se implementaron perfil mínimo de talento y experiencia, certificado de residencia, perfil de empresa, vitrina CV ciego, solicitudes y seguimiento Admin/OMIL, junto con el segundo script SQL y sus páginas Razor/API. Esta implementación inicial compila, pero no cuenta aún con pruebas automatizadas de extremo a extremo ni validación contra Oracle real; las brechas funcionales restantes se detallan en [ESTADO-MIGRACION.md](ESTADO-MIGRACION.md).

La revisión del prototipo confirma diferencias que afectan decisiones funcionales y técnicas:

- El backend ejecutable configura **MySQL**, mientras que el script SQL documentado y parte de la documentación describen **PostgreSQL**. No se deben trasladar consultas o procedimientos del script directamente a Oracle ni asumir que representan el esquema ejecutado.
- En el prototipo el alta de talento crea un usuario, pero no crea el perfil que el esquema exige. El formulario no solicita nombre, apellidos, comuna, RUT personal ni fecha de nacimiento; en el esquema revisado tampoco existen campos de RUT personal o fecha de nacimiento. El destino ahora permite completar un perfil mínimo pendiente por separado; educación, perfeccionamiento, competencias, idiomas y preferencias siguen pendientes.
- El perfil de talento combina datos identificatorios (nombres, apellidos y comuna) con datos laborales en la tabla `talentos`. La cuenta y sus credenciales se guardan aparte en `usuarios`; por tanto, no es correcto describir todos esos datos como una sola fila o como una identidad completa. Sí es necesario definir una separación de dominios y limitar qué información sale por cada interfaz.
- El prototipo incluye consultas de solicitudes y eliminación de usuarios de empresa que no muestran comprobación de pertenencia al recurso ni autorización por rol en sus rutas. Esas reglas deben implementarse y probarse en el destino; no se deben reproducir estas operaciones tal como están.
- La vista usada por la vitrina omite nombres y correo, pero contiene el indicador de discapacidad. Existe además otra vista que incluye nombre, correo y comuna. Hay que definir y probar una lista permitida de datos para cada audiencia, incluida la política sobre discapacidad.
- El registro de empresa en el prototipo genera y vincula la clave de empresa de forma explícita dentro de una transacción desde el arreglo validado el 20 de noviembre de 2026. Ese defecto puntual está corregido; no debe contabilizarse como pendiente. En el destino ya se exigen rubro, tipo y datos de contacto; la aprobación automática se mantiene como decisión provisional de esta iteración.

**Recomendación:** usar el prototipo como evidencia de pantallas, nombres de campos, catálogos y flujos; adoptar el destino como implementación institucional; y construir una matriz de paridad con criterios de aceptación antes de declarar cada módulo migrado. Antes de diseñar el esquema de talento hay que resolver qué datos personales son realmente requeridos y su origen legítimo. Antes de habilitar los flujos empresa-talento hay que especificar estados, autorización por recurso y contenido exacto del CV ciego.

Este análisis es técnico y funcional, no un estudio de mercado. La afirmación de que no existe una solución equivalente se considera contexto entregado por el solicitante y no fue verificada mediante un análisis comparativo de productos.

El origen de la necesidad en 2019, el V°B° tecnológico del primer trimestre de 2026 y la discontinuidad posterior también se toman como contexto proporcionado; no se revisaron actas ni el alcance formal de ese V°B°. La causa inicial —no conocer el estado real del código— ya está parcialmente superada: el prototipo se levantó en local, se inventariaron módulos y tecnologías, y se probaron los flujos locales de empresa y Admin. No está superada para una decisión de producción: la suite completa, la paridad funcional, los datos institucionales y los requisitos de operación aún requieren cierre.

## 1. Base y límites de la evaluación

Se inspeccionaron las rutas, servicios, validadores, modelos, migraciones, vistas SQL y formularios principales del prototipo; y las páginas, servicios, controladores, modelos, configuración y esquema inicial disponibles en el destino. También se consideró el inventario legado existente en `docs/MIGRATION-INVENTORY.md`.

El diagnóstico no incluyó acceso a una instancia municipal de Oracle/IIS, una licencia Devart institucional, un respaldo productivo aprobado, entrevistas con responsables de negocio ni confirmación de consumidores externos del API. Por ello:

- No se afirma que existan usuarios o datos productivos que deban migrarse. La necesidad de carga de datos debe decidirse con el municipio y con evidencia de un origen autorizado.
- No se determina la obligación legal o municipal de pedir RUT personal, fecha de nacimiento u otros atributos. No deben incorporarse por inferencia.
- La compatibilidad tecnológica consignada en `MIGRATION-SPEC.md` sirve como base de planificación; las versiones concretas de infraestructura deben confirmarse antes del despliegue.
- Las funciones descritas como presentes en el prototipo son observables en el código, pero no equivalen por sí solas a flujos completos y aceptados por usuarios.

## 2. Estado observable

| Área | Prototipo `proviemplea-greudy` | Destino `ipss-proviemplea` |
|---|---|---|
| Ingreso y autenticación | Formularios y endpoints de login/registro; bcrypt y JWT. El registro de talento no crea su fila de perfil. El inicio de sesión no consulta el estado de validación antes de emitir token. | Login/registro Razor y API JSON, JWT y administración básica de usuarios. Talento pendiente puede iniciar sesión para completar perfil y certificado; las reglas completas de estados y su auditoría siguen pendientes. |
| Talento/empleado | Servicios y pantallas para perfil, educación, experiencia, perfeccionamiento, competencias, idiomas, preferencias, archivos y solicitudes. Hay huecos entre el esquema, alta, servicios y pantallas. | Perfil mínimo, experiencia y certificado de residencia implementados. Educación, perfeccionamiento, competencias, idiomas y preferencias laborales siguen pendientes. |
| Empresa | Alta, perfil, usuarios asociados y consulta de solicitudes. Alta funcional corregida para generar UUID de empresa y persistir la relación transaccionalmente. Hay problemas de selección de catálogo y autorización en algunas operaciones. | Registro con catálogos y contacto requeridos, aprobación automática provisional y perfil básico. Administración de empresas y gestión de usuarios asociados pendientes. |
| CV ciego y solicitudes | Hay vista y endpoints de vitrina, y flujo de solicitudes/estados, pero no todos los SQL ni controles de pertenencia son compatibles o suficientes. | Vitrina CV ciego basada en experiencia estructurada y solicitudes empresa-talento con consulta por rol y seguimiento Admin/OMIL. Transiciones aún provisionales; faltan pruebas automatizadas de privacidad y autorización por recurso. |
| Oracle y persistencia | Sequelize declara `dialect: 'mysql'`; usa `mysql2`. Un script y README describen PostgreSQL. | Modelo Devart EF Core y dos scripts bootstrap Oracle. La compilación está verificada; conexión/CRUD Oracle y licencia Devart no. Migraciones EF versionadas pendientes. |
| Interfaz exigida | React, no alineado con Razor. | Razor y Bootstrap 5.2 están presentes en el layout; no se encontró referencia a jQuery 3.6. Revisar la incorporación de esa dependencia y la política municipal sobre uso de CDN. |
| Pruebas | Hay pruebas unitarias; en la validación previa se reportaron 49 pruebas unitarias aprobadas, mientras que la suite completa no quedó confirmada. El lint del frontend reportó errores preexistentes en esa revisión. | No se encontraron proyectos o archivos de prueba C# en el árbol de código revisado. Se requiere establecer pruebas automatizadas para la vertical existente y las siguientes. |

El número de pruebas y resultados del prototipo corresponde a la validación local reportada el 20 de noviembre de 2026; no certifica el estado de producción ni reemplaza la ejecución de una suite completa sobre un entorno estable.

## 3. Análisis por módulo y solución recomendada

### 3.1 Ingreso de personas y validación municipal

**Evidencia.** `RegistroTalento.jsx` del prototipo pide correo y contraseña. `AuthService.authRegisterTalentoPOST` crea el usuario con rol Talento y estado Pendiente, pero no crea un registro de `talentos`. La migración `20260531002203-crear-talentos.js` requiere nombres, apellidos y comuna. El paso “Datos básicos” de `CompletarPerfil.jsx` guarda resumen profesional e indicador Ley 21.015, y `TalentoService.talentosPerfilPUT` solo actualiza un perfil existente. En consecuencia, la pantalla de perfil no puede completar el registro que falta. El servicio de login emite JWT al validar la contraseña sin comprobar `estado_validacion`.

Esa era la brecha en la revisión inicial. Actualmente el registro sigue creando la cuenta primero y la persona pendiente completa después el perfil mínimo y puede cargar el certificado de residencia desde su área autenticada. El esquema adicional está en `002-talent-selection.sql`; todavía no incluye los apartados de educación, competencias, idiomas, perfeccionamiento y preferencias del alcance completo.

**Solución/estado.** El destino registra la cuenta y permite que Talento pendiente complete después los datos mínimos y el certificado. Falta validar el flujo end-to-end con persistencia Oracle y cerrar una matriz de estados y operaciones. Rechazado y Suspendido no se implementaron en esta iteración. Registrar quién decidió, cuándo y, si corresponde, un motivo sigue siendo una propuesta de auditoría por aprobar.

**Datos personales por resolver.** El esquema de talento observado contiene nombres, apellidos y comuna junto a campos laborales. No contiene RUT personal ni fecha de nacimiento; tampoco se encontró un formulario o integración que los obtenga. Antes de añadirlos, acordar con el municipio su necesidad, fundamento, fuente, validación, visibilidad, retención y eventual procedimiento de corrección. La identidad de la cuenta puede modelarse separada del perfil laboral para limitar el acceso y evitar que el DTO de CV ciego la incluya accidentalmente.

### 3.2 Empleados/talentos y perfil profesional

**Evidencia.** El prototipo contempla educación, experiencia, perfeccionamiento, competencias, idiomas, preferencias, documentos y estado de contratado. La API y las pantallas no garantizan por sí solas un recorrido completo: faltan el alta de perfil y la carga inicial de datos; se observó en ejecución que llamadas del portal de talento reciben respuestas 404 cuando no existe el perfil asociado. El endpoint de actualización no verifica que haya actualizado una fila y aun así devuelve éxito.

**Solución/estado.** El destino tiene un flujo recuperable para el perfil mínimo, experiencia laboral y certificado. La siguiente paridad pendiente incluye educación, perfeccionamiento, competencias, idiomas y preferencias laborales. Mantener autorización por propietario y borrado lógico, y probar sus reglas/códigos de respuesta. Usar entidades Oracle explícitas; no reproducir literalmente las consultas PostgreSQL/MySQL del prototipo.

### 3.3 Empresas y personas de contacto

**Evidencia.** El registro legado solicita RUT y nombre de empresa, correo y contraseña, pero no el nombre de la persona de contacto. El backend usa el nombre de la empresa como `nombre_completo` del usuario asociado. El formulario no envía rubro ni tipo, por lo que ambos servicios aplican el ID `1` por defecto. El registro legado deja la cuenta de empresa aprobada automáticamente. El destino ya solicita contacto, rubro y tipo, y mantiene aprobación automática provisional según la decisión de esta iteración.

El error previo de `RETURNING id_empresa` al registrar con MySQL se corrigió en el prototipo: ahora genera UUID en la aplicación y persiste empresa y vínculo dentro de una transacción. El servicio equivalente del destino también crea los registros relacionados en transacción.

**Solución/estado.** El destino ahora exige nombre y teléfono de contacto, rubro y tipo; mantiene aprobación automática provisional conforme a la decisión de esta iteración, sin defaults silenciosos de catálogo. El perfil básico de empresa está implementado. Gestión de usuarios asociados, administración OMIL y revisión de la regla de aprobación para producción siguen pendientes.

### 3.4 Vitrina y currículum ciego

**Evidencia.** `vw_cv_ciego` se usa desde `VitrinaService` y excluye nombres y correo; pruebas del prototipo comprueban que esos campos no aparecen en algunos resultados. La vista incluye `discapacidad_ley21015`. Otra vista, `vw_talentos_disponibles`, contiene nombre completo, correo y comuna, y es consultada por el servicio administrativo. Esto no demuestra exposición a empresas por el flujo actual, pero aumenta el riesgo de reutilizar un DTO o consulta no ciega en una ruta nueva.

**Solución/estado.** El destino implementa una lista permitida con ID opaco y experiencia estructurada; omite identidad y texto libre de resumen/descripción para evitar exponer PII escrita manualmente. Aún se requieren pruebas de serialización/API que verifiquen la ausencia de identificadores directos en listado y detalle, y confirmar que cualquier campo futuro respete la misma regla. La implementación actual no muestra el indicador de discapacidad.

### 3.5 Empresas, usuarios asociados y solicitudes

**Evidencia.** En el prototipo las rutas de empresas requieren token, pero no todas verifican rol. La eliminación de un usuario de empresa toma solo el ID objetivo y no comprueba que pertenezca a la empresa del usuario que solicita la operación. Las rutas de solicitudes también requieren solo JWT; las consultas y actualizaciones por ID de solicitud no filtran por empresa, talento o rol dentro de los servicios inspeccionados.

**Solución/estado.** Las rutas nuevas del destino separan roles y los servicios limitan consultas/mutaciones al talento o empresa asociados a la identidad; Admin/OMIL gestiona el seguimiento. Faltan pruebas negativas con IDs de otra empresa/talento, completar la administración de usuarios asociados y confirmar la máquina de estados y quién puede efectuar cada transición antes de producción.

### 3.6 Archivos y documentos

**Evidencia.** El prototipo carga archivos a disco local con un límite de 5 MB y lista tipos MIME/extensiones permitidos. El servicio persiste la ruta física y la respuesta de subida incluye `ruta_archivo`; el flujo de validación municipal depende del certificado de residencia y el perfil incorpora CV. No hay evidencia en los repositorios revisados de un almacenamiento institucional compartido ni de un plan de transición de archivos a IIS/Oracle.

**Solución.** Definir almacenamiento autorizado para desarrollo, preproducción y producción, permisos, retención, tamaño/formato, descarga autenticada, análisis de contenido y manejo de archivos huérfanos. Guardar metadatos y una clave lógica de almacenamiento, no una ruta absoluta del servidor expuesta al cliente. Planear traslado y reconciliación de archivos solo si hay documentos reales autorizados para conservar.

## 4. Incompatibilidades y deuda técnica del prototipo

1. **Motor de base de datos contradictorio.** `backend/src/config/connection.js` configura Sequelize con dialecto `mysql`; `package.json` usa `mysql2`. En cambio, `backend/docs/script_bd_proviemplea.sql` usa extensiones, `SERIAL`, `UUID`, `DROP SCHEMA public CASCADE`, `plpgsql` y funciones/procedimientos de PostgreSQL. Ese script es destructivo y no debe ejecutarse contra una base compartida o institucional. Los servicios del backend además contienen `ILIKE` en la vitrina y `RETURNING` en inserciones de solicitudes, archivos y mensajes. La solución Oracle debe partir de un modelo aprobado y consultas nuevas; el script PostgreSQL no es un DDL ejecutable equivalente ni una fuente de verdad comprobada de la base MySQL.
2. **Identidad y perfil profesional acoplados.** Nombres, apellidos y comuna están en `talentos`, junto con resumen y preferencias. Correo/credenciales están en `usuarios`. No se encontraron RUT personal ni fecha de nacimiento. Esto requiere un diseño deliberado de acceso y minimización de datos, no la conclusión de que todos los datos de identidad están en una sola tabla.
3. **Reglas de acceso insuficientes en operaciones sensibles.** Rutas de solicitudes y de usuarios de empresa tienen las brechas de alcance descritas arriba. Rehacer permisos como comportamiento esperado, no preservar la implementación literal.
4. **Calidad y cobertura no certificadas.** La validación local previa acreditó pruebas unitarias y flujos puntuales de registro/login de empresa y Admin; la suite completa no quedó confirmada, y el lint del frontend ya presentaba errores. El destino necesita pruebas automatizadas de servicios, API, Oracle, roles, privacidad y flujos completos.
5. **Dependencias de entorno.** Los archivos del prototipo se guardan en disco local. El destino usa Bootstrap desde CDN y no muestra jQuery 3.6 en las referencias inspeccionadas. Confirmar política de red/CDN y completar el stack visual requerido antes de la aceptación.

## 5. Qué reutilizar y qué no

| Reutilizar como referencia | No trasladar sin rediseño |
|---|---|
| Flujos visibles, lenguaje de negocio, estados y campos de API que sigan aprobados. | Código Express/React como arquitectura de producción; consultas SQL específicas de MySQL o PostgreSQL. |
| Catálogos y valores identificables, previa reconciliación y aprobación. | El script PostgreSQL como esquema objetivo o prueba de la estructura realmente usada en MySQL. |
| Reglas útiles de validación de campos y hash bcrypt, verificadas con pruebas. | La mezcla de datos identificatorios y laborales sin acordar controles de lectura. |
| El alta transaccional de empresa con UUID explícito y las pruebas añadidas al arreglo validado. | Autorización basada solo en tener token, respuestas `SELECT *`, estados asumidos y defaults silenciosos de catálogo. |
| Documentación de flujos y las decisiones técnicas acordadas en `MIGRATION-SPEC.md`. | Funciones visuales de demo, datos ficticios o estados presentados en UI como si acreditaran persistencia/integración real. |

## 6. Ruta de trabajo propuesta y puertas de salida

| Etapa | Trabajo principal | Condición para avanzar |
|---|---|---|
| 0. Cerrar decisiones de negocio | Definir datos requeridos de persona, validación municipal, aprobación de empresas, contenido del CV ciego y alcance de solicitudes. | Decisiones registradas por el responsable municipal; sin inventar campos o reglas. |
| 1. Línea base y trazabilidad | Completar matriz ruta React/API/modelo → página Razor/servicio/entidad/API destino; etiquetar cada función como validada, incompleta, descartada o pendiente de decisión. | Cada flujo en alcance tiene dueño, regla, permisos y criterio verificable. |
| 2. Modelo de datos y Oracle | Aprobar entidades/relaciones, separación de identidad, claves, estados, auditoría, archivos y catálogos; implementar migraciones versionadas con EF Core/Devart y ensayarlas contra Oracle 19c. | Base vacía reproducible; conexión, constraints y operaciones críticas probadas con RU y proveedor aprobados. |
| 3. Vertical ingreso y validación | Completar registro, perfil inicial, login, estados, administración, archivos de residencia y permisos por estado. | Prueba end-to-end desde registro hasta aprobación/rechazo, incluyendo acceso permitido y denegado. |
| 4. Vertical talento | CRUD de perfil y antecedentes, catálogos, preferencias, documentos y disponibilidad. | Propietario puede completar y corregir el perfil; no hay éxito falso ni acceso cruzado. |
| 5. Vertical empresa y selección | Perfil, contactos/usuarios, aprobación, vitrina ciega, solicitud y seguimiento. | Pruebas por rol/propiedad, transiciones válidas y verificación de ausencia de PII en CV ciego. |
| 6. Migración de datos, si aplica | Determinar origen autorizado, transformar MySQL, mapear IDs, contraseñas y archivos; reconciliar conteos y relaciones. | Decisión documentada sobre migrar o iniciar vacío; ensayo reversible aprobado si se migran datos. |
| 7. Preproducción y operación | Confirmar IIS/Windows/Hosting Bundle, Web Deploy, secretos, Oracle, almacenamiento, monitoreo, backup y reversa. | Despliegue repetible y prueba de recuperación aceptados en infraestructura equivalente. |

Las capacidades transversales —manejo de errores, logging, API versionada JSON, JWT, antifalsificación para cookies de navegador, validación, accesibilidad y gestión de secretos— deben acompañar cada etapa; no se dejan para una fase final.

## 7. Decisiones que requieren confirmación municipal

1. **Identidad de personas:** ¿Qué campos son obligatorios para el servicio (nombre, apellidos, RUT personal, fecha de nacimiento, comuna u otros)? ¿Cuál es la fuente y el proceso de verificación? No se recomienda pedir RUT o fecha de nacimiento hasta resolver necesidad y tratamiento.
2. **Estados de acceso:** ¿Puede una persona Pendiente iniciar sesión y completar perfil/subir documentos? ¿Qué puede hacer una persona Rechazada o Suspendida? El código actual emite token tras autenticar credenciales sin aplicar el estado de validación.
3. **Empresas:** ¿El registro debe quedar aprobado automáticamente o pendiente de validación? ¿Quién es el contacto de empresa y deben seleccionarse rubro y tipo al registrarse?
4. **CV ciego:** ¿Qué campos se muestran a empresas? En particular, ¿se revela la condición/discapacidad Ley 21.015 o solo se usa para gestión municipal con consentimiento y finalidad definida?
5. **Solicitud y seguimiento:** ¿Quién puede crear, ver, editar notas y avanzar cada estado? ¿Cuáles son las transiciones válidas, los motivos de cierre y los datos visibles a la empresa y al talento?
6. **Datos y documentos existentes:** ¿El prototipo contiene datos reales que deban conservarse o se parte con una base vacía? Si hay datos reales, ¿qué respaldo y documentos pueden tratarse en el entorno de migración?
7. **Consumo de API y tokens:** ¿Hay integraciones externas o solo consumidores institucionales? Confirmar expiración, renovación/revocación y estrategia de transición desde tokens del prototipo antes de publicar contrato estable.
8. **Infraestructura:** Antes de desplegar, confirmar Windows Server/IIS 10, Hosting Bundle/ASP.NET Core Module, RU de Oracle 19c, Devart/licencia y almacenamiento de archivos institucional. El supuesto acordado permite iniciar construcción, no reemplaza esta verificación de producción.

## 8. Criterios para considerar resuelta la problemática

- Existe una matriz completa de paridad y cada requisito en alcance tiene evidencia de prueba o una exclusión aprobada.
- El registro de talento crea un flujo utilizable de punta a punta con los campos que el municipio haya aprobado; no existen cuentas huérfanas de perfil ni respuestas de éxito sin cambios persistidos.
- La identidad está protegida por diseño: APIs y vistas de empresa usan DTOs allowlist probados; la administración obtiene datos identificatorios solo con autorización.
- El aislamiento por empresa/talento se aplica en cada lectura y mutación, y existen pruebas de acceso denegado entre usuarios.
- Oracle 19c se levanta en el entorno acordado, el esquema se crea con migraciones repetibles y las operaciones principales se prueban contra el RU/proveedor aprobado.
- La decisión sobre migración de datos es explícita y respaldada por inventario/reconciliación, o se documenta que el destino parte vacío.
- Las pantallas Razor cumplen los componentes/versiones municipales acordados; JWT, protección antifalsificación, archivos y despliegue IIS pasan pruebas específicas.

## Evidencia principal revisada

- Prototipo: `backend/src/config/connection.js`, `backend/docs/script_bd_proviemplea.sql`, `backend/src/routes/`, `backend/src/services/`, `backend/src/models/`, `backend/src/migrations/`, `frontend/src/pages/auth/`, `frontend/src/pages/talento/CompletarPerfil.jsx`, `frontend/src/services/api.js`.
- Destino: `README.md`, `MIGRATION-SPEC.md`, `docs/MIGRATION-INVENTORY.md`, `docs/CONTRATO-AUTH-ADMIN.md`, `src/ProviEmplea.Web/Program.cs`, `Services/AuthService.cs`, `Services/AdminUserService.cs`, `Data/ProviEmpleaDbContext.cs`, `Data/Schema/001-authentication.sql` y páginas Razor de acceso/registro.
- El arreglo del alta de empresa en el prototipo se documenta en `backend/src/services/AuthService.js` y `backend/src/tests/unit/authService.unit.test.js`.
