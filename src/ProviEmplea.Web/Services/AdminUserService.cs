using Microsoft.EntityFrameworkCore;
using ProviEmplea.Web.Contracts.Admin;
using ProviEmplea.Web.Data;

namespace ProviEmplea.Web.Services;

public sealed class AdminUserService(
    ProviEmpleaDbContext db,
    TalentProfileService talentProfileService)
{
    public async Task<(IReadOnlyList<AdminUserSummary> Users, int Total)> GetUsersAsync(
        string? role,
        string? validationStatus,
        int page,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = db.Users
            .AsNoTracking()
            .Where(user => user.DeletedAt == null)
            .Join(db.Roles, user => user.RoleId, dbRole => dbRole.Id, (user, dbRole) => new { user, dbRole });

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.Trim().ToLowerInvariant();
            query = query.Where(item => item.dbRole.Name == normalizedRole);
        }

        if (!string.IsNullOrWhiteSpace(validationStatus))
        {
            var normalizedStatus = validationStatus.Trim();
            query = query.Where(item => item.user.ValidationStatus == normalizedStatus);
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(item => item.user.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(item => new AdminUserSummary(
                item.user.Id,
                item.user.Email,
                item.user.ValidationStatus,
                item.user.CreatedAt,
                item.dbRole.Name))
            .ToListAsync(cancellationToken);

        return (users, total);
    }

    public async Task<bool> UpdateValidationStatusAsync(
        Guid userId,
        string status,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId && candidate.DeletedAt == null,
            cancellationToken);
        if (user is null)
        {
            return false;
        }

        if (user.RoleId == 2 &&
            status == "Aprobado" &&
            !await talentProfileService.CanApproveAsync(userId, cancellationToken))
        {
            throw new TalentApprovalPrerequisiteException(
                "No se puede aprobar el talento hasta que complete sus datos y cargue el certificado de residencia.");
        }

        user.ValidationStatus = status;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class TalentApprovalPrerequisiteException(string message) : Exception(message)
{
}
