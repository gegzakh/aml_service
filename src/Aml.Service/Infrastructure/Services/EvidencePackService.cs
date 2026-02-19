using AmlOps.Backend.Application;
using AmlOps.Backend.Domain.Entities;
using AmlOps.Backend.Domain.Enums;
using AmlOps.Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AmlOps.Backend.Infrastructure.Services;

public sealed class EvidencePackService(AmlOpsDbContext db, CurrentRequestContext context) : IEvidencePackService
{
    public async Task<byte[]> GeneratePdfAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var amlCase = await db.Cases
            .AsNoTracking()
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == caseId && x.TenantId == context.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Case not found.");

        var events = await db.CaseEvents
            .AsNoTracking()
            .Where(x => x.CaseId == caseId && x.TenantId == context.TenantId)
            .OrderBy(x => x.At)
            .ToListAsync(cancellationToken);

        var attachments = await db.Attachments
            .AsNoTracking()
            .Where(x => x.CaseId == caseId && x.TenantId == context.TenantId)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken);

        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Header().Text($"Evidence Pack | {amlCase.CaseNumber}").Bold().FontSize(18);
                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Customer: {amlCase.Customer?.FullName}");
                    column.Item().Text($"Status: {amlCase.Status}");
                    column.Item().Text($"Risk: {amlCase.RiskLevel}");
                    column.Item().Text($"Decision: {amlCase.Decision ?? "N/A"}");
                    column.Item().Text($"Created: {amlCase.CreatedAt:yyyy-MM-dd HH:mm}");
                    column.Item().Text($"Closed: {(amlCase.ClosedAt.HasValue ? amlCase.ClosedAt.Value.ToString("yyyy-MM-dd HH:mm") : "N/A")}");

                    column.Item().PaddingTop(8).Text("Timeline").Bold();
                    foreach (var evt in events)
                    {
                        column.Item().Text($"[{evt.At:yyyy-MM-dd HH:mm}] {evt.Type} | Actor {evt.ActorUserId}");
                    }

                    column.Item().PaddingTop(8).Text("Attachments").Bold();
                    foreach (var file in attachments)
                    {
                        column.Item().Text($"{file.FileName} | {file.Size} bytes | SHA256 {file.Sha256 ?? "n/a"}");
                    }
                });
                page.Footer().AlignRight().Text($"Generated at {DateTimeOffset.UtcNow:O}");
            });
        }).GeneratePdf();

        db.CaseEvents.Add(new CaseEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            CaseId = caseId,
            Type = EventType.EvidencePackExported,
            ActorUserId = context.UserId,
            At = DateTimeOffset.UtcNow,
            PayloadJson = "{}"
        });

        await db.SaveChangesAsync(cancellationToken);
        return pdf;
    }
}
