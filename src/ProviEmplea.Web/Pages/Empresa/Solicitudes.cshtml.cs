using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProviEmplea.Web.Contracts.Selection;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Empresa;

[Authorize(Roles = "empresa")]
public sealed class SolicitudesModel(RecruitmentService recruitmentService) : PageModel
{
    public IReadOnlyList<RecruitmentRequestResponse> Requests { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Requests = await recruitmentService.GetCompanyRequestsAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            cancellationToken);
    }
}
