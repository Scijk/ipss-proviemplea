using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProviEmplea.Web.Contracts.Auth;

public sealed class RegisterCompanyRequest
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
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("rut_empresa")]
    [Required(ErrorMessage = "El RUT de la empresa es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El RUT de la empresa no puede superar los 20 caracteres.")]
    public string RutEmpresa { get; init; } = string.Empty;

    [JsonPropertyName("nombre_empresa")]
    [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre de la empresa debe tener al menos 3 caracteres.")]
    [MaxLength(255, ErrorMessage = "El nombre de la empresa no puede superar los 255 caracteres.")]
    public string NombreEmpresa { get; init; } = string.Empty;

    [JsonPropertyName("id_rubro")]
    [Required(ErrorMessage = "Selecciona un rubro.")]
    [Range(1, int.MaxValue, ErrorMessage = "El rubro seleccionado no es válido.")]
    public int IdRubro { get; init; }

    [JsonPropertyName("id_tipo_empresa")]
    [Required(ErrorMessage = "Selecciona un tipo de empresa.")]
    [Range(1, int.MaxValue, ErrorMessage = "El tipo de empresa seleccionado no es válido.")]
    public int IdTipoEmpresa { get; init; }

    [JsonPropertyName("nombre_contacto")]
    [Required(ErrorMessage = "El nombre de la persona de contacto es obligatorio.")]
    [MinLength(3, ErrorMessage = "El nombre de contacto debe tener al menos 3 caracteres.")]
    [MaxLength(255, ErrorMessage = "El nombre de contacto no puede superar los 255 caracteres.")]
    public string NombreContacto { get; init; } = string.Empty;

    [JsonPropertyName("telefono_contacto")]
    [Required(ErrorMessage = "El teléfono de contacto es obligatorio.")]
    [RegularExpression(@"^\+?[0-9\s().-]{8,20}$", ErrorMessage = "Ingresa un número de teléfono válido.")]
    public string TelefonoContacto { get; init; } = string.Empty;
}
