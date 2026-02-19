namespace AmlOps.Backend.Domain.Entities;

public sealed class Attachment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public string FileKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? TagsJson { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public string? Sha256 { get; set; }

    public Case? Case { get; set; }
}
