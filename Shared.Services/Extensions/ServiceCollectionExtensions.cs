using Amazon.S3;
using Amazon.SimpleEmailV2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Options;
using Shared.Services.FileStorage;
using Shared.Services.FileStorage.Intefaces;
using Shared.Services.Interfaces;
using Shared.Services.Services;
using Shared.Services.Services.Interfaces;

namespace Shared.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddOptions<S3Settings>()
            .BindConfiguration("S3")
            .Validate(s => !string.IsNullOrEmpty(s.PublicBaseUrl) && Uri.TryCreate(s.PublicBaseUrl, UriKind.Absolute, out _),
                "S3:PublicBaseUrl must be a valid absolute URL");

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IHashingService, HashingService>();
        services.AddScoped<IFileService, S3FileService>();
        services.AddScoped<IAgeTierService, AgeTierService>();
        services.AddAWSService<IAmazonSimpleEmailServiceV2>();
        services.AddAWSService<IAmazonS3>();

        return services;
    }
}
