using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProviEmplea.Web.Contracts;
using ProviEmplea.Web.Contracts.Admin;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Authorize(Roles = "admin")]
[Route("api/v1/admin/usuarios")]
public sealed class AdminUsersController(AdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<AdminUserSummary>>> GetUsers(
        [FromQuery] string? rol,
        [FromQuery(Name = "estado_validacion")] string? estadoValidacion,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || limit is < 1 or > 100)
        {
            return BadRequest(new ApiResponse<object>(
                false,
                "page debe ser mayor que cero y limit debe estar entre 1 y 100",
                null));
        }

        var result = await adminUserService.GetUsersAsync(
            rol,
            estadoValidacion,
            page,
            limit,
            cancellationToken);
        return Ok(new PagedApiResponse<AdminUserSummary>(
            true,
            result.Users,
            new PaginationMetadata(
                result.Total,
                page,
                limit,
                (int)Math.Ceiling(result.Total / (double)limit))));
    }

    [HttpPatch("{id:guid}/validar")]
    public async Task<IActionResult> UpdateValidationStatus(
        Guid id,
        [FromBody] UpdateValidationStatusRequest request,
        CancellationToken cancellationToken)
    {
        bool updated;
        try
        {
            updated = await adminUserService.UpdateValidationStatusAsync(
                id,
                request.EstadoValidacion,
                cancellationToken);
        }
        catch (TalentApprovalPrerequisiteException exception)
        {
            return Conflict(new ApiResponse<object>(false, exception.Message));
        }
        if (!updated)
        {
            return NotFound(new ApiResponse<object>(false, "Usuario no encontrado", null));
        }

        return Ok(new ApiResponse<object>(
            true,
            "Estado de validación actualizado exitosamente",
            new { }));
    }
}
