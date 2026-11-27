namespace ProviEmplea.Web.Data.Entities;

public sealed class Company
{
    public Guid Id { get; set; }
    public string TaxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int IndustryId { get; set; }
    public int CompanyTypeId { get; set; }
    public string? Presentation { get; set; }
    public string? Benefits { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<CompanyUser> Users { get; } = new List<CompanyUser>();
    public ICollection<RecruitmentRequest> RecruitmentRequests { get; } = new List<RecruitmentRequest>();
}
