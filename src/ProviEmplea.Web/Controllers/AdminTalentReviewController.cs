using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviEmplea.Web.Contracts;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Controllers;

[ApiController]
[Produces("application/json")]
[Authorize(Roles = "admin")]
[Route("api/v1/admin/talentos")]
public sealed class AdminTalentReviewController(
    TalentProfileService profileService,
    TalentDocumentService documentService) : ControllerBase
{
    [HttpGet("pendientes")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var reviews = await profileService.GetPendingReviewsAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<PendingTalentReview>>(
            true,
            "Cuentas pendientes de revisión.",
            reviews));
    }

    [HttpGet("{userId:guid}/certificado")]
    public async Task<IActionResult> DownloadProof(Guid userId, CancellationToken cancellationToken)
    {
        var document = await documentService.GetProofDocumentForAdminAsync(userId, cancellationToken);
        return document is null
            ? NotFound(new ApiResponse<object>(false, "No se encontró el certificado de residencia."))
            : PhysicalFile(
                document.FullPath!,
                "application/octet-stream",
                "certificado-residencia." + document.Extension);
    }
}
