using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Shared.DataAccess.Providers.Interfaces;
using Shared.Enums;
using Shared.Services;

namespace Shared.Microservices.Guardian;

/// <summary>
/// Declares that this action may be performed by an actor ON BEHALF OF a subject, carried by the
/// X-Acting-As header. An action without this attribute rejects the header with 400 — which is what
/// makes a client that talks to an endpoint nobody wired up fail loudly instead of writing to the
/// actor's own account with a 200.
/// </summary>
/// <remarks>
/// A hand-written IFilterFactory rather than a TypeFilterAttribute: TypeFilterAttribute passes its
/// Arguments by their boxed runtime types, and a boxed ConsentType? is either a plain ConsentType
/// (which does not match a ConsentType? parameter) or a null the argument-type scan dereferences.
/// Constructing the filter here keeps the nullable and stays the same mechanism — TypeFilterAttribute
/// is itself an IFilterFactory.
///
/// AttributeTargets.Method only. A controller-level [AcceptsSubject] would silently mark every
/// action on it, including the admin ones — the exact class of accident this phase exists to remove.
/// </remarks>
/// <param name="permission">
/// The GuardianPermission bit the actor must hold over the subject. None means "link is enough".
/// </param>
/// <param name="consent">
/// The ConsentType the subject must have granted. None means no consent gate.
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AcceptsSubjectAttribute : Attribute, IFilterFactory, IAcceptsSubjectMetadata
{
    public AcceptsSubjectAttribute(
        GuardianPermission permission = GuardianPermission.None,
        ConsentType consent = ConsentType.None)
    {
        Permission = permission;
        Consent = consent == ConsentType.None ? null : consent;
    }

    public GuardianPermission Permission { get; }

    public ConsentType? Consent { get; }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        new AcceptsSubjectFilter(
            Permission,
            Consent,
            serviceProvider.GetRequiredService<IGuardianAuthorizer>(),
            serviceProvider.GetRequiredService<IJwtPayloadProvider>(),
            serviceProvider.GetService<TimeProvider>());
}
