namespace ProviEmplea.Web.Data.Entities;

public sealed class WorkExperience
{
    public Guid Id { get; set; }
    public Guid TalentProfileId { get; set; }
    public string EmployerName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? DeletedAt { get; set; }

    public TalentProfile TalentProfile { get; set; } = null!;
}
