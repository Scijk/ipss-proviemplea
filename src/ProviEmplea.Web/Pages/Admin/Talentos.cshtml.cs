using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Admin;

[Authorize(Roles = "admin")]
public sealed class TalentosModel(
    TalentProfileService profileService,
    AdminUserService adminUserService) : PageModel
{
    public IReadOnlyList<PendingTalentReview> PendingTalents { get; private set; } = [];

    [TempData]
    public string? Notice { get; set; }

    [TempData]
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PendingTalents = await profileService.GetPendingReviewsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await adminUserService.UpdateValidationStatusAsync(
                userId,
                "Aprobado",
                cancellationToken);
            if (!updated)
            {
                Error = "No se encontró la cuenta pendiente.";
                return RedirectToPage();
            }

            Notice = "La cuenta fue aprobada y ya podrá aparecer en la vitrina.";
        }
        catch (TalentApprovalPrerequisiteException exception)
        {
            Error = exception.Message;
        }

        return RedirectToPage();
    }
}
