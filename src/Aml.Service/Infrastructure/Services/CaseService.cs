using System.Text.Json;
using AmlOps.Backend.Application;
using AmlOps.Backend.Application.Contracts;
using AmlOps.Backend.Domain.Entities;
using AmlOps.Backend.Domain.Enums;
using AmlOps.Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AmlOps.Backend.Infrastructure.Services;

public sealed class CaseService(AmlOpsDbContext db, CurrentRequestContext context) : ICaseService
{
    public async Task<IReadOnlyCollection<CaseListItemDto>> GetCasesAsync(string? status, Guid? ownerUserId, bool overdueOnly, CancellationToken cancellationToken)
    {
        var query = db.Cases
            .AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.TenantId == context.TenantId && !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CaseStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(x => x.Status == parsedStatus);
        }

        if (ownerUserId.HasValue)
        {
            query = query.Where(x => x.OwnerUserId == ownerUserId.Value);
        }

        if (overdueOnly)
        {
            query = query.Where(x => x.SlaDueAt.HasValue && x.SlaDueAt.Value < DateTimeOffset.UtcNow && x.ClosedAt == null);
        }

        var list = await query.OrderByDescending(x => x.CreatedAt).Take(500).ToListAsync(cancellationToken);
        return list.Select(ToListItem).ToArray();
    }

    public async Task<CaseDetailDto?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var amlCase = await db.Cases
            .AsNoTracking()
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == caseId && x.TenantId == context.TenantId && !x.IsDeleted, cancellationToken);

        return amlCase is null ? null : ToDetail(amlCase);
    }

    public async Task<CaseDetailDto> CreateCaseAsync(CreateCaseRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            ExternalId = $"MANUAL-{Guid.NewGuid():N}"[..15],
            FullName = request.CustomerFullName,
            IdentifiersJson = "{}"
        };

        var settings = await GetOrCreateSlaAsync(cancellationToken);

        var amlCase = new Case
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            CaseNumber = await BuildCaseNumberAsync(cancellationToken),
            Status = CaseStatus.New,
            Priority = request.Priority,
            RiskLevel = request.RiskLevel,
            CustomerId = customer.Id,
            Customer = customer,
            CreatedAt = DateTimeOffset.UtcNow,
            SlaDueAt = request.SlaDueAt ?? CalculateSlaDueAt(request.RiskLevel, settings),
            Version = 1
        };

        db.Customers.Add(customer);
        db.Cases.Add(amlCase);
        db.CaseEvents.Add(BuildEvent(amlCase.Id, EventType.CaseCreated, new { amlCase.Status, amlCase.Priority, amlCase.RiskLevel }));
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(amlCase);
    }

    public async Task<CaseDetailDto?> AssignCaseAsync(Guid caseId, AssignCaseRequest request, CancellationToken cancellationToken)
    {
        var amlCase = await LoadCaseForWrite(caseId, cancellationToken);
        if (amlCase is null) return null;

        var oldOwner = amlCase.OwnerUserId;
        amlCase.OwnerUserId = request.OwnerUserId;
        amlCase.Version++;
        db.CaseEvents.Add(BuildEvent(caseId, EventType.Assigned, new { oldOwner, newOwner = request.OwnerUserId }));
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(amlCase);
    }

    public async Task<CaseDetailDto?> UpdateStatusAsync(Guid caseId, UpdateCaseStatusRequest request, CancellationToken cancellationToken)
    {
        var amlCase = await LoadCaseForWrite(caseId, cancellationToken);
        if (amlCase is null) return null;

        var from = amlCase.Status;
        amlCase.Status = request.Status;
        if (request.Status is CaseStatus.Closed or CaseStatus.Rejected)
        {
            amlCase.ClosedAt = DateTimeOffset.UtcNow;
        }

        amlCase.Version++;
        db.CaseEvents.Add(BuildEvent(caseId, EventType.StatusChanged, new { from, to = request.Status }));
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(amlCase);
    }

    public async Task<CaseDetailDto?> SetDecisionAsync(Guid caseId, SetDecisionRequest request, CancellationToken cancellationToken)
    {
        var amlCase = await LoadCaseForWrite(caseId, cancellationToken);
        if (amlCase is null) return null;

        amlCase.Decision = request.Decision;
        amlCase.DecisionReason = request.Reason;
        amlCase.RiskLevel = request.RiskLevel;
        amlCase.DecisionBy = context.UserId;
        amlCase.Version++;

        db.CaseEvents.Add(BuildEvent(caseId, EventType.DecisionSet, new
        {
            request.Decision,
            request.Reason,
            request.RiskLevel
        }));

        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(amlCase);
    }

    public async Task<CaseDetailDto?> ApproveCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var amlCase = await LoadCaseForWrite(caseId, cancellationToken);
        if (amlCase is null) return null;

        amlCase.ApprovedBy = context.UserId;
        amlCase.ApprovedAt = DateTimeOffset.UtcNow;
        amlCase.Status = CaseStatus.Approved;
        amlCase.Version++;

        db.CaseEvents.Add(BuildEvent(caseId, EventType.CaseApproved, new { amlCase.ApprovedBy, amlCase.ApprovedAt }));
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(amlCase);
    }

    public async Task<CaseDetailDto?> CloseCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var amlCase = await LoadCaseForWrite(caseId, cancellationToken);
        if (amlCase is null) return null;

        if (string.IsNullOrWhiteSpace(amlCase.Decision) || string.IsNullOrWhiteSpace(amlCase.DecisionReason))
        {
            throw new InvalidOperationException("Decision and reason are required before closure.");
        }

        amlCase.Status = CaseStatus.Closed;
        amlCase.ClosedAt = DateTimeOffset.UtcNow;
        amlCase.Version++;

        db.CaseEvents.Add(BuildEvent(caseId, EventType.CaseClosed, new { amlCase.Decision, amlCase.DecisionReason }));
        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(amlCase);
    }

    public async Task<CaseCommentDto?> AddCommentAsync(Guid caseId, AddCommentRequest request, CancellationToken cancellationToken)
    {
        var exists = await db.Cases.AnyAsync(x => x.Id == caseId && x.TenantId == context.TenantId && !x.IsDeleted, cancellationToken);
        if (!exists) return null;

        var comment = new CaseComment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            CaseId = caseId,
            ActorUserId = context.UserId,
            At = DateTimeOffset.UtcNow,
            Text = request.Text
        };

        db.CaseComments.Add(comment);
        db.CaseEvents.Add(BuildEvent(caseId, EventType.CommentAdded, new { request.Text }));
        await db.SaveChangesAsync(cancellationToken);

        return new CaseCommentDto(comment.Id, comment.ActorUserId, comment.At, comment.Text);
    }

    public async Task<IReadOnlyCollection<CaseCommentDto>> GetCommentsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var comments = await db.CaseComments
            .AsNoTracking()
            .Where(x => x.CaseId == caseId && x.TenantId == context.TenantId)
            .OrderByDescending(x => x.At)
            .ToListAsync(cancellationToken);

        return comments.Select(x => new CaseCommentDto(x.Id, x.ActorUserId, x.At, x.Text)).ToArray();
    }

    public async Task<IReadOnlyCollection<CaseEventDto>> GetTimelineAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var events = await db.CaseEvents
            .AsNoTracking()
            .Where(x => x.CaseId == caseId && x.TenantId == context.TenantId)
            .OrderByDescending(x => x.At)
            .ToListAsync(cancellationToken);

        return events.Select(x => new CaseEventDto(x.Id, x.Type, x.ActorUserId, x.At, x.PayloadJson)).ToArray();
    }

    public async Task<AttachmentDto?> AddAttachmentAsync(Guid caseId, CompleteAttachmentRequest request, CancellationToken cancellationToken)
    {
        var exists = await db.Cases.AnyAsync(x => x.Id == caseId && x.TenantId == context.TenantId && !x.IsDeleted, cancellationToken);
        if (!exists) return null;

        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            CaseId = caseId,
            FileKey = request.FileKey,
            FileName = request.FileName,
            ContentType = request.ContentType,
            Size = request.Size,
            TagsJson = request.TagsJson,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedBy = context.UserId,
            Sha256 = request.Sha256
        };

        db.Attachments.Add(attachment);
        db.CaseEvents.Add(BuildEvent(caseId, EventType.EvidenceAdded, new { request.FileName, request.Size, request.Sha256 }));
        await db.SaveChangesAsync(cancellationToken);

        return ToAttachmentDto(attachment);
    }

    public async Task<IReadOnlyCollection<AttachmentDto>> GetAttachmentsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var attachments = await db.Attachments
            .AsNoTracking()
            .Where(x => x.CaseId == caseId && x.TenantId == context.TenantId)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(cancellationToken);

        return attachments.Select(ToAttachmentDto).ToArray();
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var cases = await db.Cases
            .AsNoTracking()
            .Where(x => x.TenantId == context.TenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var open = cases.Count(x => x.ClosedAt == null);
        var overdue = cases.Count(x => x.ClosedAt == null && x.SlaDueAt.HasValue && x.SlaDueAt.Value < DateTimeOffset.UtcNow);
        var inReview = cases.Count(x => x.Status == CaseStatus.InReview);
        var highRisk = cases.Count(x => x.RiskLevel == RiskLevel.High);

        var closed = cases.Where(x => x.ClosedAt.HasValue).ToList();
        var avgCloseHours = closed.Count == 0
            ? 0
            : (decimal)closed.Average(x => (x.ClosedAt!.Value - x.CreatedAt).TotalHours);

        var byStatus = cases
            .GroupBy(x => x.Status.ToString())
            .Select(x => new StatusBreakdownDto(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToArray();

        var byAnalyst = cases
            .Where(x => x.OwnerUserId.HasValue)
            .GroupBy(x => x.OwnerUserId!.Value.ToString())
            .Select(x => new AnalystBreakdownDto(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToArray();

        return new DashboardDto(open, overdue, inReview, highRisk, Math.Round(avgCloseHours, 2), byStatus, byAnalyst);
    }

    private async Task<Case?> LoadCaseForWrite(Guid caseId, CancellationToken cancellationToken)
    {
        return await db.Cases
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == caseId && x.TenantId == context.TenantId && !x.IsDeleted, cancellationToken);
    }

    private async Task<string> BuildCaseNumberAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await db.Cases.CountAsync(x => x.TenantId == context.TenantId, cancellationToken) + 1;
        return $"CAS-{today}-{count:0000}";
    }

    private async Task<SlaSettings> GetOrCreateSlaAsync(CancellationToken cancellationToken)
    {
        var settings = await db.SlaSettings.FirstOrDefaultAsync(x => x.TenantId == context.TenantId, cancellationToken);
        if (settings is not null) return settings;

        settings = new SlaSettings
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.SlaSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static DateTimeOffset CalculateSlaDueAt(RiskLevel risk, SlaSettings settings)
    {
        var hours = risk switch
        {
            RiskLevel.High => settings.HighRiskHours,
            RiskLevel.Medium => settings.MediumRiskHours,
            _ => settings.LowRiskHours
        };

        return DateTimeOffset.UtcNow.AddHours(hours);
    }

    private CaseEvent BuildEvent(Guid caseId, string type, object payload)
    {
        return new CaseEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            CaseId = caseId,
            Type = type,
            ActorUserId = context.UserId,
            At = DateTimeOffset.UtcNow,
            PayloadJson = JsonSerializer.Serialize(payload)
        };
    }

    private static CaseListItemDto ToListItem(Case x)
    {
        return new CaseListItemDto(
            x.Id,
            x.CaseNumber,
            x.Status,
            x.RiskLevel,
            x.Priority,
            x.CreatedAt,
            x.SlaDueAt,
            x.OwnerUserId,
            x.Customer?.FullName ?? "-",
            x.ClosedAt == null && x.SlaDueAt.HasValue && x.SlaDueAt.Value < DateTimeOffset.UtcNow
        );
    }

    private static CaseDetailDto ToDetail(Case x)
    {
        return new CaseDetailDto(
            x.Id,
            x.CaseNumber,
            x.Status,
            x.RiskLevel,
            x.Priority,
            x.OwnerUserId,
            x.CustomerId,
            x.Customer?.FullName ?? "-",
            x.Decision,
            x.DecisionReason,
            x.CreatedAt,
            x.ClosedAt,
            x.SlaDueAt
        );
    }

    private static AttachmentDto ToAttachmentDto(Attachment x)
    {
        return new AttachmentDto(x.Id, x.FileKey, x.FileName, x.ContentType, x.Size, x.TagsJson, x.Sha256, x.UploadedAt, x.UploadedBy);
    }
}
