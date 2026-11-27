namespace ProviEmplea.Web.Data.Entities;

public sealed class RecruitmentRequest
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid TalentProfileId { get; set; }
    public int StatusId { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Company Company { get; set; } = null!;
    public TalentProfile TalentProfile { get; set; } = null!;
    public RecruitmentStatus Status { get; set; } = null!;
}
