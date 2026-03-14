using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Shared.Logging.Redaction;

namespace Shared.Logging.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PII redaction services for structured logging.
    /// </summary>
    public static IServiceCollection AddSharedLogging(this IServiceCollection services)
    {
        services.AddRedaction(options =>
        {
            options.SetRedactor<PersonalDataRedactor>(
                new DataClassificationSet(DataTaxonomy.PersonalData));
        });

        return services;
    }
}
