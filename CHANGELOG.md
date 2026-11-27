# Changelog

Los cambios relevantes del proyecto se registran aquí. Las fechas corresponden al registro de avance, no a una versión liberada.

## [Unreleased] - 2026-11-26

### Añadido

- Primera vertical funcional para perfil mínimo de talento, certificado de residencia, perfil de empresa, vitrina CV ciego y solicitudes con seguimiento Admin/OMIL.
- Script Oracle bootstrap `002-talent-selection.sql`.
- [Estado de la migración](docs/ESTADO-MIGRACION.md), con funcionalidades implementadas, brechas, decisiones provisionales y validaciones realizadas.

### Cambiado

- Registro de empresa requiere rubro, tipo y datos de contacto; conserva la aprobación automática acordada provisionalmente.
- CV ciego excluye campos de texto libre para reducir el riesgo de exposición accidental de PII.
- README y documentación de contrato/entorno actualizados para describir el estado funcional y los pasos de esquema.

### Pendiente de validación

- Operaciones contra Oracle/Devart, migraciones EF versionadas, pruebas automatizadas y paridad completa con el prototipo.
