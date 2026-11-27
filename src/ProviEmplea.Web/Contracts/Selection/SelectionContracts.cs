using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProviEmplea.Web.Contracts.Selection;

public sealed class CreateRecruitmentRequest
{
    [JsonPropertyName("id_talento")]
    [Required(ErrorMessage = "Debes seleccionar un perfil de talento.")]
    public Guid IdTalento { get; init; }
}

public sealed class UpdateRecruitmentRequest
{
    [JsonPropertyName("estado")]
    [Required(ErrorMessage = "El estado es obligatorio.")]
    [RegularExpression("^(Contactado|Entrevista|Seleccionado|No seleccionado|Cerrado)$",
        ErrorMessage = "El estado de seguimiento no es válido.")]
    public string Estado { get; init; } = string.Empty;

    [JsonPropertyName("notas_internas")]
    [MaxLength(4000, ErrorMessage = "Las notas no pueden superar los 4000 caracteres.")]
    public string? NotasInternas { get; init; }
}

public sealed record BlindTalentResponse(
    [property: JsonPropertyName("id_talento")] Guid IdTalento,
    [property: JsonPropertyName("experiencia")] IReadOnlyList<BlindWorkExperienceResponse> Experiencia);

public sealed record BlindWorkExperienceResponse(
    [property: JsonPropertyName("lugar_trabajo")] string LugarTrabajo,
    [property: JsonPropertyName("cargo")] string Cargo,
    [property: JsonPropertyName("fecha_inicio")] DateTime? FechaInicio,
    [property: JsonPropertyName("fecha_fin")] DateTime? FechaFin);

public sealed record RecruitmentRequestResponse(
    [property: JsonPropertyName("id_solicitud")] Guid IdSolicitud,
    [property: JsonPropertyName("id_talento")] Guid IdTalento,
    [property: JsonPropertyName("empresa")] string Empresa,
    [property: JsonPropertyName("estado")] string Estado,
    [property: JsonPropertyName("fecha_solicitud")] DateTime FechaSolicitud,
    [property: JsonPropertyName("fecha_actualizacion")] DateTime FechaActualizacion);

public sealed record AdminRecruitmentRequestResponse(
    [property: JsonPropertyName("id_solicitud")] Guid IdSolicitud,
    [property: JsonPropertyName("id_talento")] Guid IdTalento,
    [property: JsonPropertyName("empresa")] string Empresa,
    [property: JsonPropertyName("rut_empresa")] string RutEmpresa,
    [property: JsonPropertyName("estado")] string Estado,
    [property: JsonPropertyName("notas_internas")] string? NotasInternas,
    [property: JsonPropertyName("fecha_solicitud")] DateTime FechaSolicitud,
    [property: JsonPropertyName("fecha_actualizacion")] DateTime FechaActualizacion);
