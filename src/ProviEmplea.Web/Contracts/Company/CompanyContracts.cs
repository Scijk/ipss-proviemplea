using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProviEmplea.Web.Contracts.Company;

public sealed class UpdateCompanyProfileRequest
{
    [JsonPropertyName("nombre_empresa")]
    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")]
    [MaxLength(255, ErrorMessage = "El nombre no puede superar los 255 caracteres.")]
    public string NombreEmpresa { get; init; } = string.Empty;

    [JsonPropertyName("presentacion")]
    [MaxLength(2000, ErrorMessage = "La presentación no puede superar los 2000 caracteres.")]
    public string? Presentacion { get; init; }

    [JsonPropertyName("beneficios")]
    [MaxLength(2000, ErrorMessage = "Los beneficios no pueden superar los 2000 caracteres.")]
    public string? Beneficios { get; init; }

    [JsonPropertyName("nombre_contacto")]
    [Required(ErrorMessage = "El nombre de contacto es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre de contacto debe tener al menos 3 caracteres.")]
    [MaxLength(255, ErrorMessage = "El nombre de contacto no puede superar los 255 caracteres.")]
    public string NombreContacto { get; init; } = string.Empty;

    [JsonPropertyName("telefono_contacto")]
    [Required(ErrorMessage = "El teléfono de contacto es obligatorio.")]
    [RegularExpression(@"^\+?[0-9\s().-]{8,20}$", ErrorMessage = "Ingresa un número de teléfono válido.")]
    public string TelefonoContacto { get; init; } = string.Empty;
}

public sealed record CompanyProfileResponse(
    [property: JsonPropertyName("id_empresa")] Guid IdEmpresa,
    [property: JsonPropertyName("rut_empresa")] string RutEmpresa,
    [property: JsonPropertyName("nombre_empresa")] string NombreEmpresa,
    [property: JsonPropertyName("presentacion")] string? Presentacion,
    [property: JsonPropertyName("beneficios")] string? Beneficios,
    [property: JsonPropertyName("correo_contacto")] string CorreoContacto,
    [property: JsonPropertyName("nombre_contacto")] string NombreContacto,
    [property: JsonPropertyName("telefono_contacto")] string? TelefonoContacto);
