using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProviEmplea.Web.Contracts.Auth;

public sealed class RegisterTalentRequest
{
    [JsonPropertyName("correo")]
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    [MaxLength(255, ErrorMessage = "El correo electrónico no puede superar los 255 caracteres.")]
    public string Correo { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [MaxLength(128, ErrorMessage = "La contraseña no puede superar los 128 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "La contraseña debe tener al menos una mayúscula y un número.")]
    public string Password { get; init; } = string.Empty;
}
