using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Interfaces;
using Shared.Services.UserProfile;

namespace Shared.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserProfileGrpcClient(this IServiceCollection services, string eventsServiceUrl)
    {
        services.AddSingleton<IUserProfileGrpcService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<UserProfileGrpcClient>>();
            return new UserProfileGrpcClient(eventsServiceUrl, logger);
        });
        
        return services;
    }
}