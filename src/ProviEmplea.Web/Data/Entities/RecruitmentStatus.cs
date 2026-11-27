namespace ProviEmplea.Web.Data.Entities;

public sealed class RecruitmentStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<RecruitmentRequest> Requests { get; } = new List<RecruitmentRequest>();
}
