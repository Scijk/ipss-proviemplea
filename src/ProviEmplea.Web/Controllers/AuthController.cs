using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProviEmplea.Web.Contracts;
using ProviEmplea.Web.Contracts.Auth;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.LoginAsync(request, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new ApiResponse<object>(
                false,
                "Correo o contraseña incorrectos",
                null));
        }

        var token = await authService.CreateTokenAsync(user, cancellationToken);
        return Ok(new ApiResponse<LoginData>(
            true,
            "Login exitoso",
            new LoginData(token, user)));
    }

    [HttpPost("register/talento")]
    [EnableRateLimiting("auth-write")]
    [ProducesResponseType(typeof(ApiResponse<RegisteredUser>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterTalent(
        [FromBody] RegisterTalentRequest request,
        CancellationToken cancellationToken)
    {
        var registered = await authService.RegisterTalentAsync(request, cancellationToken);
        if (registered is null)
        {
            return Conflict(new ApiResponse<object>(false, "El correo ya está registrado", null));
        }

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<RegisteredUser>(
            true,
            "Registro exitoso. Tu cuenta será validada por el equipo municipal.",
            registered));
    }

    [HttpPost("register/empresa")]
    [EnableRateLimiting("auth-write")]
    [ProducesResponseType(typeof(ApiResponse<RegisteredUser>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterCompany(
        [FromBody] RegisterCompanyRequest request,
        CancellationToken cancellationToken)
    {
        RegisteredUser? registered;
        try
        {
            registered = await authService.RegisterCompanyAsync(request, cancellationToken);
        }
        catch (InvalidRegistrationReferenceException exception)
        {
            return BadRequest(new ApiResponse<object>(false, exception.Message, null));
        }

        if (registered is null)
        {
            return Conflict(new ApiResponse<object>(
                false,
                "El correo o RUT de empresa ya está registrado",
                null));
        }

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<RegisteredUser>(
            true,
            "Empresa registrada exitosamente.",
            registered));
    }
}
