using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using ProviEmplea.Web.Contracts.Talent;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Talento;

[Authorize(Roles = "talento")]
[EnableRateLimiting("auth-write")]
public sealed class PerfilModel(
    TalentProfileService profileService,
    TalentDocumentService documentService) : PageModel
{
    [BindProperty, ValidateNever]
    public SaveTalentProfileRequest Input { get; set; } = new();

    [BindProperty, ValidateNever]
    public SaveWorkExperienceRequest Experience { get; set; } = new();

    [BindProperty]
    public IFormFile? Certificado { get; set; }

    public TalentProfileResponse? Profile { get; private set; }
    public IReadOnlyList<WorkExperienceResponse> Experiences { get; private set; } = [];
    public string Email => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public string ValidationStatus => User.FindFirstValue("estado_validacion") ?? "Pendiente";

    [TempData]
    public string? Notice { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!TryValidateModel(Input, nameof(Input)))
        {
            await LoadAsync(cancellationToken, populateInput: false);
            return Page();
        }

        var saved = await profileService.SaveProfileAsync(CurrentUserId, Input, cancellationToken);
        if (saved.Error is not null)
        {
            ModelState.AddModelError(string.Empty, saved.Error);
            await LoadAsync(cancellationToken, populateInput: false);
            return Page();
        }

        if (Certificado is not null)
        {
            var uploaded = await documentService.UploadProofOfResidenceAsync(
                CurrentUserId,
                Certificado,
                cancellationToken);
            if (uploaded.Error is not null)
            {
                ModelState.AddModelError(string.Empty, uploaded.Error);
                await LoadAsync(cancellationToken, populateInput: false);
                return Page();
            }
        }

        Notice = "Tus datos se guardaron. La OMIL revisará el certificado y validará tu cuenta.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExperienceAsync(
        Guid? id,
        CancellationToken cancellationToken)
    {
        if (!TryValidateModel(Experience, nameof(Experience)))
        {
            await LoadAsync(cancellationToken, populateInput: false);
            return Page();
        }

        var result = id.HasValue
            ? await profileService.UpdateWorkExperienceAsync(CurrentUserId, id.Value, Experience, cancellationToken)
            : await profileService.AddWorkExperienceAsync(CurrentUserId, Experience, cancellationToken);
        if (result.Error is not null)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            await LoadAsync(cancellationToken);
            return Page();
        }

        Notice = id.HasValue ? "Experiencia actualizada." : "Experiencia agregada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteExperienceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await profileService.DeleteWorkExperienceAsync(CurrentUserId, id, cancellationToken);
        Notice = deleted ? "Experiencia eliminada." : null;
        if (!deleted)
        {
            ModelState.AddModelError(string.Empty, "No se encontró la experiencia.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken, bool populateInput = true)
    {
        Profile = await profileService.GetProfileAsync(CurrentUserId, cancellationToken);
        Experiences = await profileService.GetWorkExperiencesAsync(CurrentUserId, cancellationToken);
        if (populateInput && Profile is not null)
        {
            Input = new SaveTalentProfileRequest
            {
                NombreCompleto = Profile.NombreCompleto,
                Rut = Profile.Rut,
                Telefono = Profile.Telefono,
                Resumen = Profile.Resumen
            };
        }
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
