using AmlOps.Backend.Application.Contracts;

namespace AmlOps.Backend.Application;

public interface IAdminService
{
    Task<SlaSettingsDto> GetSlaSettingsAsync(CancellationToken cancellationToken);
    Task<SlaSettingsDto> UpdateSlaSettingsAsync(UpdateSlaSettingsRequest request, CancellationToken cancellationToken);
}
