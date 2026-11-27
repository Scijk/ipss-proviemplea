using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using ProviEmplea.Web.Contracts.Auth;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Registro;

[EnableRateLimiting("auth-write")]
public sealed class TalentoModel(AuthService authService) : PageModel
{
    [BindProperty]
    public RegistrationInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var registered = await authService.RegisterTalentAsync(
            new RegisterTalentRequest
            {
                Correo = Input.Correo,
                Password = Input.Password
            },
            cancellationToken);

        if (registered is null)
        {
            ModelState.AddModelError(string.Empty, "Este correo ya está registrado.");
            return Page();
        }

        TempData["RegistrationNotice"] =
            "Tu cuenta fue creada. Quedará pendiente de validación municipal; ya puedes iniciar sesión.";
        return RedirectToPage("/Login");
    }

    public sealed class RegistrationInput
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
        [MaxLength(255, ErrorMessage = "El correo electrónico no puede superar los 255 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [MaxLength(128, ErrorMessage = "La contraseña no puede superar los 128 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Usa al menos 8 caracteres, una mayúscula y un número.")]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}
