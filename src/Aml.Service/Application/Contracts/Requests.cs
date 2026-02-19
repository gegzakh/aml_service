using AmlOps.Backend.Domain.Enums;

namespace AmlOps.Backend.Application.Contracts;

public sealed record CreateCaseRequest(string CustomerFullName, int Priority, RiskLevel RiskLevel, DateTimeOffset? SlaDueAt);
public sealed record AssignCaseRequest(Guid OwnerUserId);
public sealed record UpdateCaseStatusRequest(CaseStatus Status);
public sealed record SetDecisionRequest(string Decision, string Reason, RiskLevel RiskLevel);
public sealed record AddCommentRequest(string Text);

public sealed record CompleteAttachmentRequest(
    string FileKey,
    string FileName,
    string ContentType,
    long Size,
    string? TagsJson,
    string? Sha256
);

public sealed record ImportResultDto(int Imported, int Skipped, int CreatedCases);
public sealed record UpdateSlaSettingsRequest(int LowRiskHours, int MediumRiskHours, int HighRiskHours);
public sealed record SlaSettingsDto(int LowRiskHours, int MediumRiskHours, int HighRiskHours);
