namespace ProviEmplea.Web.Data.Entities;

public sealed class TalentProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<WorkExperience> WorkExperiences { get; } = new List<WorkExperience>();
    public ICollection<TalentDocument> Documents { get; } = new List<TalentDocument>();
}
