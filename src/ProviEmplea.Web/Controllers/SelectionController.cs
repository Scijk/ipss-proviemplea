using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviEmplea.Web.Contracts;
using ProviEmplea.Web.Contracts.Selection;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1")]
public sealed class SelectionController(RecruitmentService recruitmentService) : ControllerBase
{
    [HttpGet("empresa/vitrina")]
    [Authorize(Roles = "empresa")]
    public async Task<IActionResult> GetBlindProfiles(CancellationToken cancellationToken)
    {
        var profiles = await recruitmentService.GetBlindProfilesAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<BlindTalentResponse>>(true, "Perfiles disponibles.", profiles));
    }

    [HttpGet("empresa/vitrina/{id:guid}")]
    [Authorize(Roles = "empresa")]
    public async Task<IActionResult> GetBlindProfile(Guid id, CancellationToken cancellationToken)
    {
        var profile = await recruitmentService.GetBlindProfileAsync(id, cancellationToken);
        return profile is null
            ? NotFound(new ApiResponse<object>(false, "El perfil no está disponible en la vitrina."))
            : Ok(new ApiResponse<BlindTalentResponse>(true, "Perfil CV ciego obtenido.", profile));
    }

    [HttpPost("empresa/solicitudes")]
    [Authorize(Roles = "empresa")]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateRecruitmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await recruitmentService.CreateRequestAsync(CurrentUserId, request.IdTalento, cancellationToken);
        if (result.Error is not null)
        {
            return Conflict(new ApiResponse<object>(false, result.Error));
        }

        return StatusCode(StatusCodes.Status201Created,
            new ApiResponse<object>(true, "Solicitud enviada a revisión de OMIL.", new { IdSolicitud = result.RequestId }));
    }

    [HttpGet("empresa/solicitudes")]
    [Authorize(Roles = "empresa")]
    public async Task<IActionResult> GetCompanyRequests(CancellationToken cancellationToken)
    {
        var requests = await recruitmentService.GetCompanyRequestsAsync(CurrentUserId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<RecruitmentRequestResponse>>(
            true,
            "Solicitudes de la empresa obtenidas.",
            requests));
    }

    [HttpGet("talento/solicitudes")]
    [Authorize(Roles = "talento")]
    public async Task<IActionResult> GetTalentRequests(CancellationToken cancellationToken)
    {
        var requests = await recruitmentService.GetTalentRequestsAsync(CurrentUserId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<RecruitmentRequestResponse>>(
            true,
            "Solicitudes recibidas obtenidas.",
            requests));
    }

    [HttpGet("admin/solicitudes")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllRequests(CancellationToken cancellationToken)
    {
        var requests = await recruitmentService.GetAllRequestsAsync(cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<AdminRecruitmentRequestResponse>>(
            true,
            "Solicitudes para seguimiento OMIL.",
            requests));
    }

    [HttpPatch("admin/solicitudes/{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateRequest(
        Guid id,
        [FromBody] UpdateRecruitmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await recruitmentService.UpdateRequestAsync(
            id,
            request.Estado,
            request.NotasInternas,
            cancellationToken);
        if (result.Error == "Solicitud no encontrada.")
        {
            return NotFound(new ApiResponse<object>(false, result.Error));
        }

        return result.Updated
            ? Ok(new ApiResponse<object>(true, "Seguimiento actualizado."))
            : BadRequest(new ApiResponse<object>(false, result.Error!));
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
