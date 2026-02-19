using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using AmlOps.Backend.Application;
using AmlOps.Backend.Application.Contracts;
using AmlOps.Backend.Domain.Entities;
using AmlOps.Backend.Domain.Enums;
using AmlOps.Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AmlOps.Backend.Infrastructure.Services;

public sealed class ImportService(AmlOpsDbContext db, CurrentRequestContext context) : IImportService
{
    public async Task<ImportResultDto> ImportAlertsCsvAsync(Stream csvStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            IgnoreBlankLines = true
        });

        var rows = csv.GetRecords<ImportRow>().ToList();

        var imported = 0;
        var skipped = 0;
        var createdCases = 0;

        var sla = await db.SlaSettings.FirstOrDefaultAsync(x => x.TenantId == context.TenantId, cancellationToken)
            ?? new SlaSettings { Id = Guid.NewGuid(), TenantId = context.TenantId, UpdatedAt = DateTimeOffset.UtcNow };

        if (sla.Id != Guid.Empty && !await db.SlaSettings.AnyAsync(x => x.Id == sla.Id, cancellationToken))
        {
            db.SlaSettings.Add(sla);
        }

        foreach (var row in rows)
        {
            var duplicate = await db.ImportedAlerts.AnyAsync(
                x => x.TenantId == context.TenantId && x.ExternalAlertId == row.ExternalAlertId,
                cancellationToken);

            if (duplicate)
            {
                skipped++;
                continue;
            }

            var customer = db.Customers.Local.FirstOrDefault(
                x => x.TenantId == context.TenantId && x.ExternalId == row.CustomerExternalId)
                ?? await db.Customers.FirstOrDefaultAsync(
                    x => x.TenantId == context.TenantId && x.ExternalId == row.CustomerExternalId,
                    cancellationToken);

            if (customer is null)
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    ExternalId = row.CustomerExternalId,
                    FullName = row.CustomerName,
                    IdentifiersJson = "{}"
                };

                db.Customers.Add(customer);
            }

            var openCase = db.Cases.Local.FirstOrDefault(
                x => x.TenantId == context.TenantId
                     && x.CustomerId == customer.Id
                     && x.ClosedAt == null
                     && !x.IsDeleted)
                ?? await db.Cases.FirstOrDefaultAsync(
                    x => x.TenantId == context.TenantId
                         && x.CustomerId == customer.Id
                         && x.ClosedAt == null
                         && !x.IsDeleted,
                    cancellationToken);

            if (openCase is null)
            {
                openCase = new Case
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    CaseNumber = $"CAS-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                    Status = CaseStatus.New,
                    Priority = 3,
                    RiskLevel = MapRisk(row.RiskHint),
                    CustomerId = customer.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    SlaDueAt = DateTimeOffset.UtcNow.AddHours(MapRiskHours(MapRisk(row.RiskHint), sla)),
                    Version = 1
                };
                db.Cases.Add(openCase);
                createdCases++;
            }

            var importedAlert = new ImportedAlert
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                ExternalAlertId = row.ExternalAlertId,
                CustomerExternalId = row.CustomerExternalId,
                AlertType = row.AlertType,
                AlertDate = row.AlertDate,
                RiskHint = row.RiskHint,
                Description = row.Description,
                CaseId = openCase.Id,
                ImportedAt = DateTimeOffset.UtcNow
            };

            db.ImportedAlerts.Add(importedAlert);
            db.CaseEvents.Add(new CaseEvent
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                CaseId = openCase.Id,
                Type = EventType.AlertsImported,
                ActorUserId = context.UserId,
                At = DateTimeOffset.UtcNow,
                PayloadJson = $"{{\"externalAlertId\":\"{row.ExternalAlertId}\",\"alertType\":\"{row.AlertType}\"}}"
            });

            imported++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ImportResultDto(imported, skipped, createdCases);
    }

    private static RiskLevel MapRisk(string? riskHint)
    {
        return riskHint?.Trim().ToLowerInvariant() switch
        {
            "high" => RiskLevel.High,
            "low" => RiskLevel.Low,
            _ => RiskLevel.Medium
        };
    }

    private static int MapRiskHours(RiskLevel risk, SlaSettings sla)
    {
        return risk switch
        {
            RiskLevel.High => sla.HighRiskHours,
            RiskLevel.Medium => sla.MediumRiskHours,
            _ => sla.LowRiskHours
        };
    }

    private sealed class ImportRow
    {
        public string ExternalAlertId { get; set; } = string.Empty;
        public string CustomerExternalId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public DateTimeOffset AlertDate { get; set; }
        public string? RiskHint { get; set; }
        public string? Description { get; set; }
    }
}
