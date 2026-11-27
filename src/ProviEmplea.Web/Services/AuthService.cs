using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BC = BCrypt.Net.BCrypt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProviEmplea.Web.Configuration;
using ProviEmplea.Web.Contracts.Auth;
using ProviEmplea.Web.Data;
using ProviEmplea.Web.Data.Entities;

namespace ProviEmplea.Web.Services;

public sealed class AuthService(
    ProviEmpleaDbContext db,
    IOptions<JwtSettings> jwtSettings)
{
    private const int TalentRoleId = 2;
    private const int CompanyRoleId = 3;
    private const string PendingStatus = "Pendiente";
    private const string ApprovedStatus = "Aprobado";

    public async Task<AuthenticatedUser?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Correo);
        var user = await db.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Email == email && candidate.DeletedAt == null,
                cancellationToken);

        if (user is null || !BC.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return new AuthenticatedUser(user.Id, user.Email, user.RoleId, user.ValidationStatus);
    }

    public async Task<string> CreateTokenAsync(AuthenticatedUser user, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Where(candidate => candidate.Id == user.IdRol)
            .Select(candidate => candidate.Name)
            .SingleAsync(cancellationToken);
        var settings = jwtSettings.Value;
        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.IdUsuario.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()),
            new Claim(ClaimTypes.Email, user.Correo),
            new Claim(ClaimTypes.Role, role),
            new Claim("id_usuario", user.IdUsuario.ToString()),
            new Claim("id_rol", user.IdRol.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim("estado_validacion", user.EstadoValidacion)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(settings.ExpiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RegisteredUser?> RegisterTalentAsync(
        RegisterTalentRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Correo);
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            RoleId = TalentRoleId,
            Email = email,
            PasswordHash = BC.HashPassword(request.Password, workFactor: 10),
            ValidationStatus = PendingStatus
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return new RegisteredUser(user.Id, user.Email, user.ValidationStatus);
    }

    public async Task<RegisteredUser?> RegisterCompanyAsync(
        RegisterCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Correo);
        var taxId = request.RutEmpresa.Trim();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken) ||
            await db.Companies.AnyAsync(company => company.TaxId == taxId, cancellationToken))
        {
            return null;
        }

        var industryId = request.IdRubro;
        var companyTypeId = request.IdTipoEmpresa;
        var referencesExist = await db.Industries.AnyAsync(item => item.Id == industryId, cancellationToken) &&
                              await db.CompanyTypes.AnyAsync(item => item.Id == companyTypeId, cancellationToken);
        if (!referencesExist)
        {
            throw new InvalidRegistrationReferenceException(
                "El rubro o tipo de empresa indicado no existe.");
        }

        var passwordHash = BC.HashPassword(request.Password, workFactor: 10);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = new User
        {
            Id = Guid.NewGuid(),
            RoleId = CompanyRoleId,
            Email = email,
            PasswordHash = passwordHash,
            ValidationStatus = ApprovedStatus
        };
        var company = new Company
        {
            Id = Guid.NewGuid(),
            TaxId = taxId,
            Name = request.NombreEmpresa.Trim(),
            IndustryId = industryId,
            CompanyTypeId = companyTypeId
        };
        db.Users.Add(user);
        db.Companies.Add(company);
        db.CompanyUsers.Add(new CompanyUser
        {
            UserId = user.Id,
            CompanyId = company.Id,
            FullName = request.NombreContacto.Trim(),
            ContactPhone = request.TelefonoContacto.Trim()
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new RegisteredUser(user.Id, user.Email, user.ValidationStatus);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

public sealed class InvalidRegistrationReferenceException(string message) : Exception(message)
{
}
