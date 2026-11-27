using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ProviEmplea.Web.Configuration;
using ProviEmplea.Web.Contracts.Auth;
using ProviEmplea.Web.Services;

namespace ProviEmplea.Web.Pages;

[EnableRateLimiting("login")]
public sealed class LoginModel(
    AuthService authService,
    IOptions<JwtSettings> jwtSettings,
    IWebHostEnvironment environment) : PageModel
{
    [BindProperty]
    public LoginRequest Input { get; set; } = new();

    [TempData]
    public string? RegistrationNotice { get; set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await authService.LoginAsync(Input, cancellationToken);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return Page();
        }

        var token = await authService.CreateTokenAsync(user, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtSettings.Value.ExpiresMinutes);
        Response.Cookies.Append(AuthCookie.Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expiresAt,
            MaxAge = expiresAt - DateTimeOffset.UtcNow,
            IsEssential = true
        });

        return user.IdRol switch
        {
            1 => RedirectToPage("/Admin/Talentos"),
            2 => RedirectToPage("/Talento/Perfil"),
            3 => RedirectToPage("/Empresa/Perfil"),
            _ => RedirectToPage("/Index")
        };
    }

}
