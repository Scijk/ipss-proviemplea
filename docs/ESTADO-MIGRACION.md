# Estado de la migración

**Actualizado:** 26 de noviembre de 2026  
**Base:** estado del repositorio destino y del spec; el inventario del prototipo está en [MIGRATION-INVENTORY.md](MIGRATION-INVENTORY.md).

## Resumen

El destino cuenta con una primera vertical funcional de autenticación, talento, empresa y selección. Compila con .NET 10 y las páginas públicas principales respondieron correctamente en una prueba local sin Oracle. Esto no acredita persistencia funcional: Oracle/Devart, las operaciones con datos y los flujos de extremo a extremo aún no se han probado en una base Oracle real.

## Implementado

- Login y registro de talento/empresa en Razor y API JSON, autenticación JWT y navegación por rol.
- Registro de empresa con rubro, tipo y persona de contacto requeridos; aprobación automática provisional.
- Perfil mínimo de talento pendiente: nombre completo, RUT, teléfono y resumen opcional; alta, edición y eliminación lógica de experiencia laboral.
- Carga del certificado de residencia (PDF, Word, JPG o PNG, hasta 5 MB) a almacenamiento local fuera de `wwwroot`; acceso restringido al titular o Admin.
- Perfil de empresa y contacto.
- Vitrina y detalle de CV ciego para talentos aprobados con experiencia. La respuesta permite experiencia estructurada y omite identidad y texto libre.
- Solicitudes empresa-talento, consulta por empresa/talento y seguimiento Admin/OMIL.
- Revisión Admin de talentos pendientes y descarga del certificado.
- Scripts bootstrap Oracle `001-authentication.sql` y `002-talent-selection.sql`, en ese orden.
- Guía local, contrato API, README y exclusión de archivos subidos en Git actualizados.

## Pendiente frente al prototipo y el spec

| Área | Pendiente |
|---|---|
| Perfil laboral de talento | Educación, perfeccionamiento, competencias, idiomas y preferencias laborales; definir qué otros documentos forman parte de esta entrega. |
| Administración | Gestión de empresas por Admin/OMIL y gestión de usuarios asociados a una empresa. |
| Solicitudes | Confirmar el grafo de transiciones; completar paridad de detalle, historial y notas conforme al contrato aprobado. |
| Catálogos | Implementar los catálogos que requieran los formularios de talento y definir su administración. |
| Pruebas y privacidad | Añadir pruebas automatizadas unitarias, API e integración para autorización por recurso, CV ciego, estados, RUT y archivos. Las descripciones libres se excluyen del CV ciego; validar el resto de rutas y serializaciones. |
| Oracle y esquema | Probar scripts, EF/Devart y operaciones CRUD contra Oracle. Sustituir bootstrap de ejecución única por migraciones EF versionadas revisables. |
| Migración de datos | Acordar si hay datos/archivos autorizados que migrar; preparar herramienta repetible, reconciliación y rollback. |
| Plataforma y despliegue | Preparar Docker Compose cuando estén disponibles medios Oracle autorizados; validar IIS/Web Deploy, configuración, permisos y recuperación. |
| Paridad visual/funcional | Completar trazabilidad de pantallas, endpoints, validaciones y permisos del prototipo y validar con responsables funcionales. |

Mensajería, dashboards, estadísticas y exportaciones generales siguen fuera del primer alcance según el spec, salvo que se acuerde una dependencia.

## Decisiones aún provisionales

- La empresa queda aprobada automáticamente en esta iteración.
- Rechazado y Suspendido no forman parte del flujo actual de talento.
- Las solicitudes usan `Solicitado`, `Contactado`, `Entrevista`, `Seleccionado`, `No seleccionado` y `Cerrado`; el grafo de transiciones requiere confirmación.
- El CV ciego no incluye resumen ni descripciones de texto libre para reducir el riesgo de exposición accidental de datos identificatorios.

## Validación realizada

- `dotnet build ProviEmplea.slnx --no-restore -p:UseAppHost=false -p:OutputPath=bin\FeatureBuild\`: correcto, cero advertencias y cero errores.
- Prueba HTTP local en `Development`, sin Oracle: `/health/live`, `/login`, `/registro/talento` y `/registro/empresa` respondieron `200`.
- `git diff --check`: sin errores de formato.

No se ejecutaron pruebas automatizadas de aplicación ni pruebas de integración/CRUD contra Oracle. Las páginas autenticadas y las funciones dependientes de datos requieren una validación adicional con configuración local y base disponible.

## Documentación relacionada

- [Guía del entorno local](GUIA-ENTORNO-LOCAL.md)
- [Contrato inicial API](CONTRATO-AUTH-ADMIN.md)
- [Inventario del prototipo](MIGRATION-INVENTORY.md)
- [Análisis de solución](ANALISIS-SOLUCION-PROPUESTA.md)
- [Spec rector](../MIGRATION-SPEC.md)
