using AmlOps.Backend.Domain.Enums;

namespace AmlOps.Backend.Application.Contracts;

public sealed record CaseListItemDto(
    Guid Id,
    string CaseNumber,
    CaseStatus Status,
    RiskLevel RiskLevel,
    int Priority,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SlaDueAt,
    Guid? OwnerUserId,
    string CustomerFullName,
    bool IsOverdue
);

public sealed record CaseDetailDto(
    Guid Id,
    string CaseNumber,
    CaseStatus Status,
    RiskLevel RiskLevel,
    int Priority,
    Guid? OwnerUserId,
    Guid CustomerId,
    string CustomerFullName,
    string? Decision,
    string? DecisionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? SlaDueAt
);

public sealed record CaseEventDto(
    Guid Id,
    string Type,
    Guid ActorUserId,
    DateTimeOffset At,
    string PayloadJson
);

public sealed record CaseCommentDto(
    Guid Id,
    Guid ActorUserId,
    DateTimeOffset At,
    string Text
);

public sealed record AttachmentDto(
    Guid Id,
    string FileKey,
    string FileName,
    string ContentType,
    long Size,
    string? TagsJson,
    string? Sha256,
    DateTimeOffset UploadedAt,
    Guid UploadedBy
);

public sealed record DashboardDto(
    int OpenCases,
    int OverdueCases,
    int InReviewCases,
    int HighRiskCases,
    decimal AverageHoursToClose,
    IReadOnlyCollection<StatusBreakdownDto> ByStatus,
    IReadOnlyCollection<AnalystBreakdownDto> ByAnalyst
);

public sealed record StatusBreakdownDto(string Status, int Count);
public sealed record AnalystBreakdownDto(string OwnerUserId, int Count);
