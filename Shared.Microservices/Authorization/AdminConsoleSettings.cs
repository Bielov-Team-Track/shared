namespace Shared.Microservices.Authorization;

/// <summary>
/// Credentials for the endpoints the admin console calls.
/// </summary>
/// <remarks>
/// A shared secret rather than a JWT claim, deliberately. The platform has no platform-admin
/// concept and should not gain one: if admin authority were a claim in a user token, a stolen
/// or forged user token could become an admin token. This secret is held only by the console.
/// </remarks>
public class AdminConsoleSettings
{
    public const string SectionName = "AdminConsole";

    public string ApiKey { get; set; } = string.Empty;
}
