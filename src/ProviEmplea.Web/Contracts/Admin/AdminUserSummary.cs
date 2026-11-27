using System.Text.Json.Serialization;

namespace ProviEmplea.Web.Contracts.Admin;

public sealed record AdminUserSummary(
    [property: JsonPropertyName("id_usuario")] Guid IdUsuario,
    [property: JsonPropertyName("correo")] string Correo,
    [property: JsonPropertyName("estado_validacion")] string EstadoValidacion,
    [property: JsonPropertyName("fecha_creacion")] DateTime FechaCreacion,
    [property: JsonPropertyName("rol")] string Rol);
