namespace AmlOps.Backend.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string IdentifiersJson { get; set; } = "{}";
    public string? Country { get; set; }
    public DateOnly? Dob { get; set; }
    public string? RiskFlagsJson { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<Case> Cases { get; set; } = new List<Case>();
}
