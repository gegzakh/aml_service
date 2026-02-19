using AmlOps.Backend.Domain.Entities;
using AmlOps.Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AmlOps.Backend.Infrastructure.Persistence;

public static class DemoSeed
{
    public static async Task SeedAsync(AmlOpsDbContext db)
    {
        if (await db.Cases.AnyAsync())
        {
            return;
        }

        var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var analyst = Guid.Parse("00000000-0000-0000-0000-000000000111");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalId = "CUST-1001",
            FullName = "Amina Rahman",
            IdentifiersJson = "{\"customerNo\":\"CUST-1001\"}"
        };

        var amlCase = new Case
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseNumber = "CAS-20260218-1001",
            Status = CaseStatus.InReview,
            Priority = 3,
            RiskLevel = RiskLevel.High,
            CustomerId = customer.Id,
            Customer = customer,
            OwnerUserId = analyst,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-5),
            SlaDueAt = DateTimeOffset.UtcNow.AddHours(12),
            Version = 1
        };

        db.Cases.Add(amlCase);
        db.CaseEvents.Add(new CaseEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseId = amlCase.Id,
            Type = EventType.CaseCreated,
            ActorUserId = analyst,
            At = amlCase.CreatedAt,
            PayloadJson = "{}"
        });

        db.SlaSettings.Add(new SlaSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LowRiskHours = 72,
            MediumRiskHours = 48,
            HighRiskHours = 24,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
