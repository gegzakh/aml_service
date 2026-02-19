namespace AmlOps.Backend.Domain.Entities;

public sealed class CaseComment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset At { get; set; }
    public string Text { get; set; } = string.Empty;

    public Case? Case { get; set; }
}
