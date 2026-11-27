using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProviEmplea.Web.Contracts.Selection;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Admin;

[Authorize(Roles = "admin")]
public sealed class SolicitudesModel(RecruitmentService recruitmentService) : PageModel
{
    public IReadOnlyList<AdminRecruitmentRequestResponse> Requests { get; private set; } = [];

    [TempData]
    public string? Notice { get; set; }

    [TempData]
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Requests = await recruitmentService.GetAllRequestsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        Guid id,
        string estado,
        string? notasInternas,
        CancellationToken cancellationToken)
    {
        var result = await recruitmentService.UpdateRequestAsync(
            id,
            estado,
            notasInternas,
            cancellationToken);
        if (result.Updated)
        {
            Notice = "El seguimiento fue actualizado.";
        }
        else
        {
            Error = result.Error;
        }

        return RedirectToPage();
    }
}
