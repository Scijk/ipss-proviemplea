using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using ProviEmplea.Web.Contracts.Auth;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Registro;

[EnableRateLimiting("auth-write")]
public sealed class EmpresaModel(AuthService authService, CompanyService companyService) : PageModel
{
    [BindProperty]
    public RegistrationInput Input { get; set; } = new();

    public IReadOnlyList<CompanyCatalogOption> Industries { get; private set; } = [];
    public IReadOnlyList<CompanyCatalogOption> CompanyTypes { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadCatalogsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCatalogsAsync(cancellationToken);
            return Page();
        }

        RegisteredUser? registered;
        try
        {
            registered = await authService.RegisterCompanyAsync(
                new RegisterCompanyRequest
                {
                    Correo = Input.Correo,
                    Password = Input.Password,
                    RutEmpresa = Input.RutEmpresa,
                    NombreEmpresa = Input.NombreEmpresa,
                    IdRubro = Input.IdRubro,
                    IdTipoEmpresa = Input.IdTipoEmpresa,
                    NombreContacto = Input.NombreContacto,
                    TelefonoContacto = Input.TelefonoContacto
                },
                cancellationToken);
        }
        catch (InvalidRegistrationReferenceException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadCatalogsAsync(cancellationToken);
            return Page();
        }

        if (registered is null)
        {
            ModelState.AddModelError(string.Empty, "El correo o RUT de empresa ya está registrado.");
            await LoadCatalogsAsync(cancellationToken);
            return Page();
        }

        TempData["RegistrationNotice"] =
            "La cuenta de empresa fue creada. Ya puedes iniciar sesión.";
        return RedirectToPage("/Login");
    }

    private async Task LoadCatalogsAsync(CancellationToken cancellationToken)
    {
        var catalogs = await companyService.GetRegistrationCatalogsAsync(cancellationToken);
        Industries = catalogs.Industries;
        CompanyTypes = catalogs.Types;
    }

    public sealed class RegistrationInput
    {
        [Required(ErrorMessage = "El correo de contacto es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
        [MaxLength(255, ErrorMessage = "El correo de contacto no puede superar los 255 caracteres.")]
        [Display(Name = "Correo de contacto")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [MaxLength(128, ErrorMessage = "La contraseña no puede superar los 128 caracteres.")]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUT de la empresa es obligatorio.")]
        [MaxLength(20, ErrorMessage = "El RUT de la empresa no puede superar los 20 caracteres.")]
        [Display(Name = "RUT de la empresa")]
        public string RutEmpresa { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
        [MinLength(3, ErrorMessage = "El nombre de la empresa debe tener al menos 3 caracteres.")]
        [MaxLength(255, ErrorMessage = "El nombre de la empresa no puede superar los 255 caracteres.")]
        [Display(Name = "Nombre de la empresa")]
        public string NombreEmpresa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona un rubro.")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un rubro válido.")]
        [Display(Name = "Rubro")]
        public int IdRubro { get; set; }

        [Required(ErrorMessage = "Selecciona un tipo de empresa.")]
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un tipo de empresa válido.")]
        [Display(Name = "Tipo de empresa")]
        public int IdTipoEmpresa { get; set; }

        [Required(ErrorMessage = "El nombre de la persona de contacto es obligatorio.")]
        [MinLength(3, ErrorMessage = "El nombre de contacto debe tener al menos 3 caracteres.")]
        [MaxLength(255, ErrorMessage = "El nombre de contacto no puede superar los 255 caracteres.")]
        [Display(Name = "Nombre de la persona de contacto")]
        public string NombreContacto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono de contacto es obligatorio.")]
        [RegularExpression(@"^\+?[0-9\s().-]{8,20}$", ErrorMessage = "Ingresa un número de teléfono válido.")]
        [Display(Name = "Teléfono de contacto")]
        public string TelefonoContacto { get; set; } = string.Empty;
    }
}
