using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProviEmplea.Web.Contracts.Admin;

public sealed class UpdateValidationStatusRequest
{
    [JsonPropertyName("estado_validacion")]
    [Required(ErrorMessage = "El estado de validación es obligatorio.")]
    [RegularExpression("^(Pendiente|Aprobado|Rechazado)$",
        ErrorMessage = "El estado debe ser Pendiente, Aprobado o Rechazado.")]
    public string EstadoValidacion { get; init; } = string.Empty;
}
