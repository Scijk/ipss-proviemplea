using System.Text.Json.Serialization;

namespace ProviEmplea.Web.Contracts.Auth;

public sealed record AuthenticatedUser(
    [property: JsonPropertyName("id_usuario")] Guid IdUsuario,
    [property: JsonPropertyName("correo")] string Correo,
    [property: JsonPropertyName("id_rol")] int IdRol,
    [property: JsonPropertyName("estado_validacion")] string EstadoValidacion);

public sealed record LoginData(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("usuario")] AuthenticatedUser Usuario);

public sealed record RegisteredUser(
    [property: JsonPropertyName("id_usuario")] Guid IdUsuario,
    [property: JsonPropertyName("correo")] string Correo,
    [property: JsonPropertyName("estado_validacion")] string EstadoValidacion);
