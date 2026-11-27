namespace ProviEmplea.Web.Data.Entities;

public sealed class CompanyUser
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }

    public User User { get; set; } = null!;
    public Company Company { get; set; } = null!;
}
