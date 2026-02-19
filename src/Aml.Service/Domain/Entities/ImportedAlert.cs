namespace AmlOps.Backend.Domain.Entities;

public sealed class ImportedAlert
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ExternalAlertId { get; set; } = string.Empty;
    public string CustomerExternalId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public DateTimeOffset AlertDate { get; set; }
    public string? RiskHint { get; set; }
    public string? Description { get; set; }
    public Guid CaseId { get; set; }
    public DateTimeOffset ImportedAt { get; set; }
}
