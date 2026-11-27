using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProviEmplea.Web.Contracts.Selection;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages.Empresa;

[Authorize(Roles = "empresa")]
public sealed class VitrinaModel(RecruitmentService recruitmentService) : PageModel
{
    public IReadOnlyList<BlindTalentResponse> Profiles { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Profiles = await recruitmentService.GetBlindProfilesAsync(cancellationToken);
    }
}
