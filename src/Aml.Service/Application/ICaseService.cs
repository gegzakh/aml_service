using AmlOps.Backend.Application.Contracts;

namespace AmlOps.Backend.Application;

public interface ICaseService
{
    Task<IReadOnlyCollection<CaseListItemDto>> GetCasesAsync(string? status, Guid? ownerUserId, bool overdueOnly, CancellationToken cancellationToken);
    Task<CaseDetailDto?> GetCaseAsync(Guid caseId, CancellationToken cancellationToken);
    Task<CaseDetailDto> CreateCaseAsync(CreateCaseRequest request, CancellationToken cancellationToken);
    Task<CaseDetailDto?> AssignCaseAsync(Guid caseId, AssignCaseRequest request, CancellationToken cancellationToken);
    Task<CaseDetailDto?> UpdateStatusAsync(Guid caseId, UpdateCaseStatusRequest request, CancellationToken cancellationToken);
    Task<CaseDetailDto?> SetDecisionAsync(Guid caseId, SetDecisionRequest request, CancellationToken cancellationToken);
    Task<CaseDetailDto?> ApproveCaseAsync(Guid caseId, CancellationToken cancellationToken);
    Task<CaseDetailDto?> CloseCaseAsync(Guid caseId, CancellationToken cancellationToken);
    Task<CaseCommentDto?> AddCommentAsync(Guid caseId, AddCommentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CaseCommentDto>> GetCommentsAsync(Guid caseId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CaseEventDto>> GetTimelineAsync(Guid caseId, CancellationToken cancellationToken);
    Task<AttachmentDto?> AddAttachmentAsync(Guid caseId, CompleteAttachmentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AttachmentDto>> GetAttachmentsAsync(Guid caseId, CancellationToken cancellationToken);
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
}
