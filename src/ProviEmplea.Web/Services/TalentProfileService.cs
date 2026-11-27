using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProviEmplea.Web.Contracts.Talent;
using ProviEmplea.Web.Data;
using ProviEmplea.Web.Data.Entities;

namespace ProviEmplea.Web.Services;

public sealed class TalentProfileService(ProviEmpleaDbContext db)
{
    public async Task<TalentProfileResponse?> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await db.TalentProfiles
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.DeletedAt == null)
            .Select(item => new
            {
                item.FullName,
                item.NationalId,
                item.Phone,
                item.Summary,
                item.User.Email,
                HasProof = item.Documents.Any(document =>
                    document.DocumentType == TalentDocumentService.ProofOfResidenceType &&
                    document.DeletedAt == null)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return profile is null
            ? null
            : new TalentProfileResponse(
                profile.FullName,
                profile.NationalId,
                profile.Phone,
                profile.Email,
                profile.Summary,
                profile.HasProof);
    }

    public async Task<(TalentProfileResponse? Profile, string? Error)> SaveProfileAsync(
        Guid userId,
        SaveTalentProfileRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedRut = ChileanNationalId.NormalizeAndValidate(request.Rut);
        if (normalizedRut is null)
        {
            return (null, "Ingresa un RUT chileno válido.");
        }

        var profile = await db.TalentProfiles
            .SingleOrDefaultAsync(
                item => item.UserId == userId && item.DeletedAt == null,
                cancellationToken);

        var duplicateRut = await db.TalentProfiles.AnyAsync(
            item => item.NationalId == normalizedRut && item.UserId != userId && item.DeletedAt == null,
            cancellationToken);
        if (duplicateRut)
        {
            return (null, "El RUT ya está asociado a otro perfil.");
        }

        if (profile is null)
        {
            profile = new TalentProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            db.TalentProfiles.Add(profile);
        }

        profile.FullName = request.NombreCompleto.Trim();
        profile.NationalId = normalizedRut;
        profile.Phone = request.Telefono.Trim();
        profile.Summary = string.IsNullOrWhiteSpace(request.Resumen) ? null : request.Resumen.Trim();

        await db.SaveChangesAsync(cancellationToken);
        return (await GetProfileAsync(userId, cancellationToken), null);
    }

    public async Task<IReadOnlyList<WorkExperienceResponse>> GetWorkExperiencesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await db.WorkExperiences
            .AsNoTracking()
            .Where(item => item.TalentProfile.UserId == userId &&
                           item.TalentProfile.DeletedAt == null &&
                           item.DeletedAt == null)
            .OrderByDescending(item => item.StartDate)
            .Select(item => new WorkExperienceResponse(
                item.Id,
                item.EmployerName,
                item.JobTitle,
                item.Description,
                item.StartDate,
                item.EndDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<(WorkExperienceResponse? Experience, string? Error)> AddWorkExperienceAsync(
        Guid userId,
        SaveWorkExperienceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FechaInicio.HasValue &&
            request.FechaFin.HasValue &&
            request.FechaFin.Value.Date < request.FechaInicio.Value.Date)
        {
            return (null, "La fecha de término no puede ser anterior a la fecha de inicio.");
        }

        var profileId = await db.TalentProfiles
            .Where(item => item.UserId == userId && item.DeletedAt == null)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (profileId is null)
        {
            return (null, "Completa primero los datos básicos de tu perfil.");
        }

        var experience = new WorkExperience
        {
            Id = Guid.NewGuid(),
            TalentProfileId = profileId.Value,
            EmployerName = request.Empresa.Trim(),
            JobTitle = request.Cargo.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim(),
            StartDate = request.FechaInicio?.Date,
            EndDate = request.FechaFin?.Date
        };
        db.WorkExperiences.Add(experience);
        await db.SaveChangesAsync(cancellationToken);

        return (new WorkExperienceResponse(
            experience.Id,
            experience.EmployerName,
            experience.JobTitle,
            experience.Description,
            experience.StartDate,
            experience.EndDate), null);
    }

    public async Task<(WorkExperienceResponse? Experience, string? Error)> UpdateWorkExperienceAsync(
        Guid userId,
        Guid experienceId,
        SaveWorkExperienceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.FechaInicio.HasValue &&
            request.FechaFin.HasValue &&
            request.FechaFin.Value.Date < request.FechaInicio.Value.Date)
        {
            return (null, "La fecha de término no puede ser anterior a la fecha de inicio.");
        }

        var experience = await db.WorkExperiences.SingleOrDefaultAsync(
            item => item.Id == experienceId &&
                    item.TalentProfile.UserId == userId &&
                    item.TalentProfile.DeletedAt == null &&
                    item.DeletedAt == null,
            cancellationToken);
        if (experience is null)
        {
            return (null, "Experiencia no encontrada.");
        }

        experience.EmployerName = request.Empresa.Trim();
        experience.JobTitle = request.Cargo.Trim();
        experience.Description = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        experience.StartDate = request.FechaInicio?.Date;
        experience.EndDate = request.FechaFin?.Date;
        await db.SaveChangesAsync(cancellationToken);

        return (new WorkExperienceResponse(
            experience.Id,
            experience.EmployerName,
            experience.JobTitle,
            experience.Description,
            experience.StartDate,
            experience.EndDate), null);
    }

    public async Task<bool> DeleteWorkExperienceAsync(
        Guid userId,
        Guid experienceId,
        CancellationToken cancellationToken)
    {
        var experience = await db.WorkExperiences.SingleOrDefaultAsync(
            item => item.Id == experienceId &&
                    item.TalentProfile.UserId == userId &&
                    item.TalentProfile.DeletedAt == null &&
                    item.DeletedAt == null,
            cancellationToken);
        if (experience is null)
        {
            return false;
        }

        experience.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PendingTalentReview>> GetPendingReviewsAsync(
        CancellationToken cancellationToken)
    {
        return await db.Users
            .AsNoTracking()
            .Where(user => user.RoleId == 2 &&
                           user.ValidationStatus == "Pendiente" &&
                           user.DeletedAt == null)
            .OrderBy(user => user.CreatedAt)
            .Select(user => new PendingTalentReview(
                user.Id,
                user.Email,
                user.TalentProfile == null ? null : user.TalentProfile.FullName,
                user.TalentProfile == null ? null : user.TalentProfile.NationalId,
                user.TalentProfile == null ? null : user.TalentProfile.Phone,
                user.TalentProfile != null &&
                user.TalentProfile.Documents.Any(document =>
                    document.DocumentType == TalentDocumentService.ProofOfResidenceType &&
                    document.DeletedAt == null)))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid?> GetLatestProofDocumentIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.TalentDocuments
            .Where(document => document.TalentProfile.UserId == userId &&
                               document.TalentProfile.DeletedAt == null &&
                               document.DocumentType == TalentDocumentService.ProofOfResidenceType &&
                               document.DeletedAt == null)
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => (Guid?)document.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> CanApproveAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.Users.AnyAsync(
            user => user.Id == userId &&
                    user.RoleId == 2 &&
                    user.ValidationStatus == "Pendiente" &&
                    user.DeletedAt == null &&
                    user.TalentProfile != null &&
                    user.TalentProfile.DeletedAt == null &&
                    user.TalentProfile.FullName != "" &&
                    user.TalentProfile.NationalId != "" &&
                    user.TalentProfile.Phone != "" &&
                    user.TalentProfile.Documents.Any(document =>
                        document.DocumentType == TalentDocumentService.ProofOfResidenceType &&
                        document.DeletedAt == null),
            cancellationToken);
    }
}

public sealed record PendingTalentReview(
    Guid UserId,
    string Email,
    string? FullName,
    string? NationalId,
    string? Phone,
    bool HasProofOfResidence);

public static partial class ChileanNationalId
{
    [GeneratedRegex(@"[.\-\s]", RegexOptions.CultureInvariant)]
    private static partial Regex Separators();

    public static string? NormalizeAndValidate(string value)
    {
        var normalized = Separators().Replace(value, string.Empty).ToUpperInvariant();
        if (normalized.Length is < 8 or > 9 ||
            !Regex.IsMatch(normalized, @"^\d{7,8}[0-9K]$", RegexOptions.CultureInvariant))
        {
            return null;
        }

        var body = normalized[..^1];
        var sum = 0;
        var factor = 2;
        for (var index = body.Length - 1; index >= 0; index--)
        {
            sum += (body[index] - '0') * factor;
            factor = factor == 7 ? 2 : factor + 1;
        }

        var remainder = 11 - sum % 11;
        var expectedCheckDigit = remainder switch
        {
            11 => '0',
            10 => 'K',
            _ => (char)('0' + remainder)
        };

        return normalized[^1] == expectedCheckDigit ? normalized : null;
    }
}
