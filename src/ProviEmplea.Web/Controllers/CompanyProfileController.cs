using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviEmplea.Web.Contracts;
using ProviEmplea.Web.Contracts.Company;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Controllers;

[ApiController]
[Produces("application/json")]
[Authorize(Roles = "empresa")]
[Route("api/v1/empresa")]
public sealed class CompanyProfileController(CompanyService companyService) : ControllerBase
{
    [HttpGet("perfil")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await companyService.GetProfileAsync(CurrentUserId, cancellationToken);
        return profile is null
            ? NotFound(new ApiResponse<object>(false, "No se encontró el perfil de empresa."))
            : Ok(new ApiResponse<CompanyProfileResponse>(true, "Perfil de empresa obtenido.", profile));
    }

    [HttpPut("perfil")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await companyService.UpdateProfileAsync(CurrentUserId, request, cancellationToken);
        return profile is null
            ? NotFound(new ApiResponse<object>(false, "No se encontró el perfil de empresa."))
            : Ok(new ApiResponse<CompanyProfileResponse>(true, "Perfil de empresa actualizado.", profile));
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
