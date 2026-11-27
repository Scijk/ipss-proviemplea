using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using System.Data.Common;
using Devart.Data.Oracle;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProviEmplea.Web.Data;
using ProviEmplea.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddProblemDetails();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "ProviEmplea.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.AddOpenApi();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error => new
                {
                    field = entry.Key,
                    message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "El valor enviado no es válido."
                        : error.ErrorMessage
                }))
                .ToArray();

            return new BadRequestObjectResult(new
            {
                success = false,
                message = "Datos inválidos",
                errors
            });
        };
    });

var oracleConnectionString = builder.Configuration.GetConnectionString("Oracle");
var isDevelopment = builder.Environment.IsDevelopment();
if (string.IsNullOrWhiteSpace(oracleConnectionString) && !isDevelopment)
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:Oracle. Configure the local Oracle connection before starting the application.");
}

if (string.IsNullOrWhiteSpace(oracleConnectionString))
{
    Console.Error.WriteLine(
        "Development mode: Oracle is not configured. Razor pages can be previewed, but data-backed requests will not work.");
}
var oracleIsConfigured = !string.IsNullOrWhiteSpace(oracleConnectionString);
var providerConnectionString = oracleIsConfigured
    ? oracleConnectionString!
    : "Server=127.0.0.1;Port=1521;User Id=preview;Password=preview;Service Name=preview;Direct=True";

var devartLicenseKey = builder.Configuration["Devart:LicenseKey"];
if (!string.IsNullOrWhiteSpace(devartLicenseKey))
{
    OracleConnection.LoadLicenseKey(devartLicenseKey);
}

builder.Services.AddDbContext<ProviEmpleaDbContext>(options =>
    options.UseOracle(providerConnectionString));

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? (isDevelopment ? "ProviEmplea.Local" : null);
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? (isDevelopment ? "ProviEmplea.Local.Api" : null);
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
var jwtExpiresMinutes = builder.Configuration.GetValue<int?>("Jwt:ExpiresMinutes") ?? 60;

if (string.IsNullOrWhiteSpace(jwtSigningKey) && isDevelopment)
{
    jwtSigningKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    string.IsNullOrWhiteSpace(jwtSigningKey) ||
    Encoding.UTF8.GetByteCount(jwtSigningKey) < 32 ||
    jwtExpiresMinutes is < 1 or > 1440)
{
    throw new InvalidOperationException(
        "Configure Jwt:Issuer, Jwt:Audience, Jwt:SigningKey (at least 32 UTF-8 bytes), and Jwt:ExpiresMinutes (1-1440).");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue(AuthCookie.Name, out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<TalentProfileService>();
builder.Services.AddScoped<TalentDocumentService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<RecruitmentService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    Func<HttpContext, RateLimitPartition<string>> perIpPartition = context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    options.AddPolicy("login", perIpPartition);
    options.AddPolicy("auth-write", perIpPartition);
});
builder.Services.Configure<ProviEmplea.Web.Configuration.JwtSettings>(options =>
{
    options.Issuer = jwtIssuer;
    options.Audience = jwtAudience;
    options.SigningKey = jwtSigningKey;
    options.ExpiresMinutes = jwtExpiresMinutes;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    var isUnsafeMethod = !HttpMethods.IsGet(context.Request.Method) &&
                         !HttpMethods.IsHead(context.Request.Method) &&
                         !HttpMethods.IsOptions(context.Request.Method) &&
                         !HttpMethods.IsTrace(context.Request.Method);
    if (isUnsafeMethod &&
        context.Request.Cookies.ContainsKey(AuthCookie.Name) &&
        !context.Request.Headers.ContainsKey("Authorization"))
    {
        try
        {
            await context.RequestServices
                .GetRequiredService<IAntiforgery>()
                .ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "La solicitud no superó la validación antifalsificación."
            });
            return;
        }
    }

    await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async Task<IResult> (
    ProviEmpleaDbContext db,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (!oracleIsConfigured)
    {
        return Results.Problem(
            "Oracle connection is not configured.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    try
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            logger.LogWarning("Oracle readiness check could not establish a connection.");
            return Results.Problem(
                "Oracle is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new { status = "ready" });
    }
    catch (Devart.Common.LicenseException exception)
    {
        logger.LogError(exception, "Devart provider license activation failed.");
        return Results.Problem(
            "Oracle provider license is not activated.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (DbException exception)
    {
        logger.LogWarning(exception, "Oracle readiness check failed.");
        return Results.Problem(
            "Oracle is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapRazorPages();
app.MapControllers();
app.MapOpenApi();

app.Run();

public partial class Program
{
}

public static class AuthCookie
{
    public const string Name = "ProviEmplea.AccessToken";
}
