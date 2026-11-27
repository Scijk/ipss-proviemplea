using Microsoft.EntityFrameworkCore;
using ProviEmplea.Web.Contracts.Selection;
using ProviEmplea.Web.Data;
using ProviEmplea.Web.Data.Entities;

namespace ProviEmplea.Web.Services;

public sealed class RecruitmentService(ProviEmpleaDbContext db)
{
    private static readonly string[] ActiveStatuses = ["Solicitado", "Contactado", "Entrevista"];

    public async Task<IReadOnlyList<BlindTalentResponse>> GetBlindProfilesAsync(
        CancellationToken cancellationToken)
    {
        var profiles = await db.TalentProfiles
            .AsNoTracking()
            .Where(profile => profile.DeletedAt == null &&
                              profile.User.DeletedAt == null &&
                              profile.User.ValidationStatus == "Aprobado" &&
                              profile.WorkExperiences.Any(experience => experience.DeletedAt == null))
            .OrderBy(profile => profile.Id)
            .Select(profile => new BlindTalentResponse(
                profile.Id,
                profile.WorkExperiences
                    .Where(experience => experience.DeletedAt == null)
                    .OrderByDescending(experience => experience.StartDate)
                    .Select(experience => new BlindWorkExperienceResponse(
                        experience.EmployerName,
                        experience.JobTitle,
                        experience.StartDate,
                        experience.EndDate))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return profiles;
    }

    public Task<BlindTalentResponse?> GetBlindProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        return db.TalentProfiles
            .AsNoTracking()
            .Where(profile => profile.Id == profileId &&
                              profile.DeletedAt == null &&
                              profile.User.DeletedAt == null &&
                              profile.User.ValidationStatus == "Aprobado" &&
                              profile.WorkExperiences.Any(experience => experience.DeletedAt == null))
            .Select(profile => new BlindTalentResponse(
                profile.Id,
                profile.WorkExperiences
                    .Where(experience => experience.DeletedAt == null)
                    .OrderByDescending(experience => experience.StartDate)
                    .Select(experience => new BlindWorkExperienceResponse(
                        experience.EmployerName,
                        experience.JobTitle,
                        experience.StartDate,
                        experience.EndDate))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<(Guid? RequestId, string? Error)> CreateRequestAsync(
        Guid companyUserId,
        Guid talentProfileId,
        CancellationToken cancellationToken)
    {
        var companyId = await db.CompanyUsers
            .Where(membership => membership.UserId == companyUserId &&
                                membership.User.DeletedAt == null &&
                                membership.User.ValidationStatus == "Aprobado" &&
                                membership.Company.DeletedAt == null)
            .Select(membership => (Guid?)membership.CompanyId)
            .SingleOrDefaultAsync(cancellationToken);
        if (companyId is null)
        {
            return (null, "No se encontró una empresa habilitada para esta cuenta.");
        }

        var profileAvailable = await db.TalentProfiles.AnyAsync(
            profile => profile.Id == talentProfileId &&
                       profile.DeletedAt == null &&
                       profile.User.DeletedAt == null &&
                       profile.User.ValidationStatus == "Aprobado" &&
                       profile.WorkExperiences.Any(experience => experience.DeletedAt == null),
            cancellationToken);
        if (!profileAvailable)
        {
            return (null, "El perfil no está disponible en la vitrina.");
        }

        var hasActiveRequest = await db.RecruitmentRequests.AnyAsync(
            request => request.CompanyId == companyId &&
                       request.TalentProfileId == talentProfileId &&
                       ActiveStatuses.Contains(request.Status.Name),
            cancellationToken);
        if (hasActiveRequest)
        {
            return (null, "Ya existe una solicitud activa para este perfil.");
        }

        var request = new RecruitmentRequest
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId.Value,
            TalentProfileId = talentProfileId,
            StatusId = 1
        };
        db.RecruitmentRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
        return (request.Id, null);
    }

    public async Task<IReadOnlyList<RecruitmentRequestResponse>> GetCompanyRequestsAsync(
        Guid companyUserId,
        CancellationToken cancellationToken)
    {
        return await db.RecruitmentRequests
            .AsNoTracking()
            .Where(request => request.Company.Users.Any(user => user.UserId == companyUserId))
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new RecruitmentRequestResponse(
                request.Id,
                request.TalentProfileId,
                request.Company.Name,
                request.Status.Name,
                request.CreatedAt,
                request.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecruitmentRequestResponse>> GetTalentRequestsAsync(
        Guid talentUserId,
        CancellationToken cancellationToken)
    {
        return await db.RecruitmentRequests
            .AsNoTracking()
            .Where(request => request.TalentProfile.UserId == talentUserId)
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new RecruitmentRequestResponse(
                request.Id,
                request.TalentProfileId,
                request.Company.Name,
                request.Status.Name,
                request.CreatedAt,
                request.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminRecruitmentRequestResponse>> GetAllRequestsAsync(
        CancellationToken cancellationToken)
    {
        return await db.RecruitmentRequests
            .AsNoTracking()
            .OrderByDescending(request => request.CreatedAt)
            .Select(request => new AdminRecruitmentRequestResponse(
                request.Id,
                request.TalentProfileId,
                request.Company.Name,
                request.Company.TaxId,
                request.Status.Name,
                request.InternalNotes,
                request.CreatedAt,
                request.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Updated, string? Error)> UpdateRequestAsync(
        Guid requestId,
        string newStatus,
        string? internalNotes,
        CancellationToken cancellationToken)
    {
        var request = await db.RecruitmentRequests
            .Include(item => item.Status)
            .SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (request is null)
        {
            return (false, "Solicitud no encontrada.");
        }

        var status = await db.RecruitmentStatuses
            .SingleOrDefaultAsync(item => item.Name == newStatus, cancellationToken);
        if (status is null)
        {
            return (false, "El estado de seguimiento no existe.");
        }

        if (!IsTransitionAllowed(request.Status.Name, status.Name))
        {
            return (false, $"No se permite cambiar de '{request.Status.Name}' a '{status.Name}'.");
        }

        request.StatusId = status.Id;
        request.InternalNotes = string.IsNullOrWhiteSpace(internalNotes) ? null : internalNotes.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private static bool IsTransitionAllowed(string current, string next) => current switch
    {
        "Solicitado" => next is "Contactado" or "Cerrado",
        "Contactado" => next is "Entrevista" or "No seleccionado" or "Cerrado",
        "Entrevista" => next is "Seleccionado" or "No seleccionado" or "Cerrado",
        "Seleccionado" or "No seleccionado" => next == "Cerrado",
        _ => false
    };
}
