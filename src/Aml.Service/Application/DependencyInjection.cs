using AmlOps.Backend.Infrastructure;
using AmlOps.Backend.Infrastructure.Services;

namespace AmlOps.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CurrentRequestContext>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IEvidencePackService, EvidencePackService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
