using Microsoft.EntityFrameworkCore;
using ProviEmplea.Web.Contracts.Company;
using ProviEmplea.Web.Data;

namespace ProviEmplea.Web.Services;

public sealed class CompanyService(ProviEmpleaDbContext db)
{
    private static readonly IReadOnlyList<CompanyCatalogOption> RegistrationIndustries =
    [
        new(1, "Tecnología"),
        new(2, "Salud"),
        new(3, "Educación"),
        new(4, "Comercio"),
        new(5, "Construcción"),
        new(6, "Gastronomía"),
        new(7, "Finanzas"),
        new(8, "Legal"),
        new(9, "Marketing"),
        new(10, "Logística")
    ];

    private static readonly IReadOnlyList<CompanyCatalogOption> RegistrationCompanyTypes =
    [
        new(1, "Contratación directa"),
        new(2, "EST"),
        new(3, "Outsourcing")
    ];

    public Task<CompanyProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        return db.CompanyUsers
            .AsNoTracking()
            .Where(membership => membership.UserId == userId &&
                                 membership.User.DeletedAt == null &&
                                 membership.Company.DeletedAt == null)
            .Select(membership => new CompanyProfileResponse(
                membership.CompanyId,
                membership.Company.TaxId,
                membership.Company.Name,
                membership.Company.Presentation,
                membership.Company.Benefits,
                membership.User.Email,
                membership.FullName,
                membership.ContactPhone))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CompanyProfileResponse?> UpdateProfileAsync(
        Guid userId,
        UpdateCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var membership = await db.CompanyUsers
            .Include(item => item.Company)
            .SingleOrDefaultAsync(
                item => item.UserId == userId &&
                        item.User.DeletedAt == null &&
                        item.Company.DeletedAt == null,
                cancellationToken);
        if (membership is null)
        {
            return null;
        }

        membership.Company.Name = request.NombreEmpresa.Trim();
        membership.Company.Presentation = CleanOptionalText(request.Presentacion);
        membership.Company.Benefits = CleanOptionalText(request.Beneficios);
        membership.FullName = request.NombreContacto.Trim();
        membership.ContactPhone = request.TelefonoContacto.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(userId, cancellationToken);
    }

    public Task<(IReadOnlyList<CompanyCatalogOption> Industries, IReadOnlyList<CompanyCatalogOption> Types)>
        GetRegistrationCatalogsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((RegistrationIndustries, RegistrationCompanyTypes));
    }

    private static string? CleanOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record CompanyCatalogOption(int Id, string Name);
