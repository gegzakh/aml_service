namespace AmlOps.Backend.Application;

public interface IEvidencePackService
{
    Task<byte[]> GeneratePdfAsync(Guid caseId, CancellationToken cancellationToken);
}
