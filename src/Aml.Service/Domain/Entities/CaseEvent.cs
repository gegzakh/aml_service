namespace AmlOps.Backend.Domain.Entities;

public sealed class CaseEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTimeOffset At { get; set; }
    public string PayloadJson { get; set; } = "{}";

    public Case? Case { get; set; }
}
