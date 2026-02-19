using AmlOps.Backend.Application;
using AmlOps.Backend.Application.Contracts;
using AmlOps.Backend.Domain.Entities;
using AmlOps.Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AmlOps.Backend.Infrastructure.Services;

public sealed class AdminService(AmlOpsDbContext db, CurrentRequestContext context) : IAdminService
{
    public async Task<SlaSettingsDto> GetSlaSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreate(cancellationToken);
        return new SlaSettingsDto(settings.LowRiskHours, settings.MediumRiskHours, settings.HighRiskHours);
    }

    public async Task<SlaSettingsDto> UpdateSlaSettingsAsync(UpdateSlaSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreate(cancellationToken);
        settings.LowRiskHours = request.LowRiskHours;
        settings.MediumRiskHours = request.MediumRiskHours;
        settings.HighRiskHours = request.HighRiskHours;
        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new SlaSettingsDto(settings.LowRiskHours, settings.MediumRiskHours, settings.HighRiskHours);
    }

    private async Task<SlaSettings> GetOrCreate(CancellationToken cancellationToken)
    {
        var settings = await db.SlaSettings.FirstOrDefaultAsync(x => x.TenantId == context.TenantId, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

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
}
