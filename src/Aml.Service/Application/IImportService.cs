using AmlOps.Backend.Application.Contracts;

namespace AmlOps.Backend.Application;

public interface IImportService
{
    Task<ImportResultDto> ImportAlertsCsvAsync(Stream csvStream, CancellationToken cancellationToken);
}
