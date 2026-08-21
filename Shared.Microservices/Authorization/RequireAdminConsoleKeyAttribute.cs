using Microsoft.AspNetCore.Mvc;

namespace Shared.Microservices.Authorization;

/// <summary>
/// Requires the admin console's shared secret on the request. Apply to controllers whose
/// endpoints the console calls.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireAdminConsoleKeyAttribute : TypeFilterAttribute
{
    public RequireAdminConsoleKeyAttribute()
        : base(typeof(AdminConsoleKeyFilter))
    {
    }
}
