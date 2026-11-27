using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProviEmplea.Web.Contracts.Selection;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Empresa;

[Authorize(Roles = "empresa")]
public sealed class TalentoModel(RecruitmentService recruitmentService) : PageModel
{
    public BlindTalentResponse? Profile { get; private set; }

    [TempData]
    public string? Notice { get; set; }

    [TempData]
    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Profile = await recruitmentService.GetBlindProfileAsync(id, cancellationToken);
        return Profile is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await recruitmentService.CreateRequestAsync(CurrentUserId, id, cancellationToken);
        if (result.Error is not null)
        {
            Error = result.Error;
        }
        else
        {
            Notice = "La solicitud se envió a revisión de OMIL.";
        }

        return RedirectToPage(new { id });
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}
