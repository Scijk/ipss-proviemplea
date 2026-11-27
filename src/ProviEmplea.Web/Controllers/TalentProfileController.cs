using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviEmplea.Web.Contracts;
using ProviEmplea.Web.Contracts.Talent;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Controllers;

[ApiController]
[Produces("application/json")]
[Authorize(Roles = "talento")]
[Route("api/v1/talento")]
public sealed class TalentProfileController(
    TalentProfileService profileService,
    TalentDocumentService documentService) : ControllerBase
{
    [HttpGet("perfil")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await profileService.GetProfileAsync(CurrentUserId, cancellationToken);
        return Ok(new ApiResponse<TalentProfileResponse?>(
            true,
            profile is null ? "Completa los datos básicos de tu perfil." : "Perfil obtenido.",
            profile));
    }

    [HttpPut("perfil")]
    public async Task<IActionResult> SaveProfile(
        [FromBody] SaveTalentProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await profileService.SaveProfileAsync(CurrentUserId, request, cancellationToken);
        if (result.Error is not null)
        {
            return BadRequest(new ApiResponse<object>(false, result.Error));
        }

        return Ok(new ApiResponse<TalentProfileResponse?>(true, "Perfil guardado.", result.Profile));
    }

    [HttpGet("experiencia")]
    public async Task<IActionResult> GetExperiences(CancellationToken cancellationToken)
    {
        var experiences = await profileService.GetWorkExperiencesAsync(CurrentUserId, cancellationToken);
        return Ok(new ApiResponse<IReadOnlyList<WorkExperienceResponse>>(true, "Experiencia obtenida.", experiences));
    }

    [HttpPost("experiencia")]
    public async Task<IActionResult> AddExperience(
        [FromBody] SaveWorkExperienceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await profileService.AddWorkExperienceAsync(CurrentUserId, request, cancellationToken);
        if (result.Error is not null)
        {
            return BadRequest(new ApiResponse<object>(false, result.Error));
        }

        return StatusCode(StatusCodes.Status201Created,
            new ApiResponse<WorkExperienceResponse?>(true, "Experiencia agregada.", result.Experience));
    }

    [HttpPut("experiencia/{id:guid}")]
    public async Task<IActionResult> UpdateExperience(
        Guid id,
        [FromBody] SaveWorkExperienceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await profileService.UpdateWorkExperienceAsync(
            CurrentUserId,
            id,
            request,
            cancellationToken);
        if (result.Error == "Experiencia no encontrada.")
        {
            return NotFound(new ApiResponse<object>(false, result.Error));
        }

        return result.Error is not null
            ? BadRequest(new ApiResponse<object>(false, result.Error))
            : Ok(new ApiResponse<WorkExperienceResponse?>(true, "Experiencia actualizada.", result.Experience));
    }

    [HttpDelete("experiencia/{id:guid}")]
    public async Task<IActionResult> DeleteExperience(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await profileService.DeleteWorkExperienceAsync(CurrentUserId, id, cancellationToken);
        return deleted
            ? Ok(new ApiResponse<object>(true, "Experiencia eliminada."))
            : NotFound(new ApiResponse<object>(false, "Experiencia no encontrada."));
    }

    [HttpPost("certificado-residencia")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<IActionResult> UploadProof(
        [FromForm(Name = "archivo")] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var result = await documentService.UploadProofOfResidenceAsync(CurrentUserId, file, cancellationToken);
        if (result.Error is not null)
        {
            return BadRequest(new ApiResponse<object>(false, result.Error));
        }

        return StatusCode(StatusCodes.Status201Created,
            new ApiResponse<TalentDocumentResponse?>(true, "Certificado cargado.", result.Document));
    }

    [HttpGet("certificado-residencia")]
    public async Task<IActionResult> DownloadOwnProof(CancellationToken cancellationToken)
    {
        var document = await documentService.GetOwnedProofDocumentAsync(CurrentUserId, null, cancellationToken);
        return document is null
            ? NotFound(new ApiResponse<object>(false, "No se encontró el certificado cargado."))
            : PhysicalFile(
                document.FullPath!,
                "application/octet-stream",
                "certificado-residencia." + document.Extension);
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
