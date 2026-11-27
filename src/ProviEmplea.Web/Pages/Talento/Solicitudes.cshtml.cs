using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProviEmplea.Web.Contracts.Selection;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Talento;

[Authorize(Roles = "talento")]
public sealed class SolicitudesModel(RecruitmentService recruitmentService) : PageModel
{
    public IReadOnlyList<RecruitmentRequestResponse> Requests { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Requests = await recruitmentService.GetTalentRequestsAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken);
    }
}
