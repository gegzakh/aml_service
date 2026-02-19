namespace AmlOps.Backend.Domain.Entities;

public sealed class SlaSettings
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int LowRiskHours { get; set; } = 72;
    public int MediumRiskHours { get; set; } = 48;
    public int HighRiskHours { get; set; } = 24;
    public DateTimeOffset UpdatedAt { get; set; }
}
