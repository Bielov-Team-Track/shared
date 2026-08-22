using Microsoft.Extensions.DependencyInjection;
using Shared.Guardian.Hosting;
using Shared.Guardian.Interfaces;
using Shared.Guardian.Services;

namespace Shared.Guardian.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Caller must already have registered: IRepository&lt;GuardianLink&gt;, IRepository&lt;UserProfile&gt;,
    /// IDistributedCache, IAgeTierService (AddSharedServices) and an IGuardianLinkSource.
    /// Consumers are registered by the caller inside AddMessaging's bus configurator.
    /// </summary>
    public static IServiceCollection AddGuardianLinkReplica(this IServiceCollection services)
    {
        services.AddScoped<IGuardianLinkService, GuardianLinkService>();
        services.AddHostedService<GuardianLinkPruneService>();
        return services;
    }
}
