using AmlOps.Backend.Domain.Enums;

namespace AmlOps.Backend.Domain.Entities;

public sealed class Case
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.New;
    public int Priority { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;
    public Guid? OwnerUserId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTimeOffset? SlaDueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? Decision { get; set; }
    public string? DecisionReason { get; set; }
    public Guid? DecisionBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public int Version { get; set; }
    public bool IsDeleted { get; set; }

    public Customer? Customer { get; set; }
    public ICollection<CaseComment> Comments { get; set; } = new List<CaseComment>();
    public ICollection<CaseEvent> Events { get; set; } = new List<CaseEvent>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
