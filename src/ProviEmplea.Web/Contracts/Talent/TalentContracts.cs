using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProviEmplea.Web.Contracts.Talent;

public sealed class SaveTalentProfileRequest
{
    [JsonPropertyName("nombre_completo")]
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")]
    [MaxLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    public string NombreCompleto { get; init; } = string.Empty;

    [JsonPropertyName("rut")]
    [Required(ErrorMessage = "El RUT es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El RUT no puede superar los 20 caracteres.")]
    public string Rut { get; init; } = string.Empty;

    [JsonPropertyName("telefono")]
    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^\+?[0-9\s().-]{8,20}$", ErrorMessage = "Ingresa un número de teléfono válido.")]
    public string Telefono { get; init; } = string.Empty;

    [JsonPropertyName("resumen")]
    [MaxLength(2000, ErrorMessage = "El resumen no puede superar los 2000 caracteres.")]
    public string? Resumen { get; init; }
}

public sealed record TalentProfileResponse(
    [property: JsonPropertyName("nombre_completo")] string NombreCompleto,
    [property: JsonPropertyName("rut")] string Rut,
    [property: JsonPropertyName("telefono")] string Telefono,
    [property: JsonPropertyName("correo")] string Correo,
    [property: JsonPropertyName("resumen")] string? Resumen,
    [property: JsonPropertyName("certificado_cargado")] bool CertificadoCargado);

public sealed class SaveWorkExperienceRequest
{
    [JsonPropertyName("empresa")]
    [Required(ErrorMessage = "El lugar de trabajo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre del lugar de trabajo no puede superar los 150 caracteres.")]
    public string Empresa { get; init; } = string.Empty;

    [JsonPropertyName("cargo")]
    [Required(ErrorMessage = "El cargo es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El cargo no puede superar los 100 caracteres.")]
    public string Cargo { get; init; } = string.Empty;

    [JsonPropertyName("descripcion")]
    [MaxLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string? Descripcion { get; init; }

    [JsonPropertyName("fecha_inicio")]
    public DateTime? FechaInicio { get; init; }

    [JsonPropertyName("fecha_fin")]
    public DateTime? FechaFin { get; init; }
}

public sealed record WorkExperienceResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("empresa")] string Empresa,
    [property: JsonPropertyName("cargo")] string Cargo,
    [property: JsonPropertyName("descripcion")] string? Descripcion,
    [property: JsonPropertyName("fecha_inicio")] DateTime? FechaInicio,
    [property: JsonPropertyName("fecha_fin")] DateTime? FechaFin);

public sealed record TalentDocumentResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("tipo_documento")] string TipoDocumento,
    [property: JsonPropertyName("formato")] string Formato,
    [property: JsonPropertyName("tamano_bytes")] long TamanoBytes,
    [property: JsonPropertyName("fecha_subida")] DateTime FechaSubida);
