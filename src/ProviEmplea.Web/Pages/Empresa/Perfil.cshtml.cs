using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ProviEmplea.Web.Contracts.Company;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Empresa;

[Authorize(Roles = "empresa")]
public sealed class PerfilModel(CompanyService companyService) : PageModel
{
    [BindProperty, ValidateNever]
    public UpdateCompanyProfileRequest Input { get; set; } = new();

    public CompanyProfileResponse? Profile { get; private set; }

    [TempData]
    public string? Notice { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken, populateInput: false);
        return Profile is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!TryValidateModel(Input, nameof(Input)))
        {
            await LoadAsync(cancellationToken, populateInput: false);
            return Page();
        }

        var profile = await companyService.UpdateProfileAsync(CurrentUserId, Input, cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        Notice = "El perfil de empresa fue actualizado.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken, bool populateInput = true)
    {
        Profile = await companyService.GetProfileAsync(CurrentUserId, cancellationToken);
        if (populateInput && Profile is not null)
        {
            Input = new UpdateCompanyProfileRequest
            {
                NombreEmpresa = Profile.NombreEmpresa,
                Presentacion = Profile.Presentacion,
                Beneficios = Profile.Beneficios,
                NombreContacto = Profile.NombreContacto,
                TelefonoContacto = Profile.TelefonoContacto ?? string.Empty
            };
        }
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
