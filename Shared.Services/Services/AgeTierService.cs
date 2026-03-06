using Shared.Data;
using Shared.Enums;
using Shared.Services.Services.Interfaces;

namespace Shared.Services.Services;

public class AgeTierService : IAgeTierService
{
    private readonly TimeProvider _timeProvider;

    public AgeTierService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // NOTE: All 8 services that instantiate AgeTierService must register TimeProvider in DI.
    // Add `services.AddSingleton(TimeProvider.System);` to each service's Program.cs:
    //   - auth-service, profiles-service, clubs-service, events-service,
    //     messages-service, notifications-service, social-service, coaching-service
    // The optional parameter with default provides backward compat during migration.

    public AgeTier CalculateAgeTier(DateTime dateOfBirth, string countryCode, DateTime? asOfDate = null)
    {
        var age = CalculateAge(dateOfBirth, asOfDate);
        var consentAge = ConsentAgeMap.GetConsentAge(countryCode);

        if (age >= 18) return AgeTier.Adult;
        if (age >= consentAge) return AgeTier.TeenConsentTo17;
        if (age >= 13) return AgeTier.Teen13ToConsent;
        return AgeTier.Under13;
    }

    public int GetConsentAge(string countryCode) => ConsentAgeMap.GetConsentAge(countryCode);

    public int CalculateAge(DateTime dateOfBirth, DateTime? asOfDate = null)
    {
        var today = asOfDate?.Date ?? _timeProvider.GetUtcNow().UtcDateTime.Date;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    public bool IsMinor(DateTime dateOfBirth, DateTime? asOfDate = null)
        => CalculateAge(dateOfBirth, asOfDate) < 18;

    public bool CanHaveCredentials(DateTime dateOfBirth, DateTime? asOfDate = null)
        => CalculateAge(dateOfBirth, asOfDate) >= 13;

    public bool IsGuardianRequired(AgeTier tier) =>
        tier == AgeTier.Teen13ToConsent;

    public bool IsGuardianOptional(AgeTier tier) =>
        tier == AgeTier.TeenConsentTo17;
}
