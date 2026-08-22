using Shared.Enums;

namespace Shared.Services;

/// <summary>
/// Endpoint metadata marker, implemented by AcceptsSubjectAttribute in Shared.Microservices.
/// The middleware lives in Shared and cannot see that assembly, but it has to be able to ask
/// whether an endpoint opted in.
/// </summary>
public interface IAcceptsSubjectMetadata
{
    GuardianPermission Permission { get; }
    ConsentType? Consent { get; }
}
