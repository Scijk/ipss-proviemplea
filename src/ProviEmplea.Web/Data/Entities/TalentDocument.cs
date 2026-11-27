namespace ProviEmplea.Web.Data.Entities;

public sealed class TalentDocument
{
    public Guid Id { get; set; }
    public Guid TalentProfileId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public TalentProfile TalentProfile { get; set; } = null!;
}
