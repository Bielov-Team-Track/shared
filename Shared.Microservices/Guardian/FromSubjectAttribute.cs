using Microsoft.AspNetCore.Mvc;

namespace Shared.Microservices.Guardian;

/// <summary>
/// Binds the subject that [AcceptsSubject] validated. By attribute and never by parameter name,
/// so renaming the parameter cannot silently start binding it from the query string.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromSubjectAttribute : ModelBinderAttribute
{
    public FromSubjectAttribute() : base(typeof(SubjectUserIdModelBinder))
    {
    }
}
