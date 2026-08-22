using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Services;

namespace Shared.Microservices.Guardian;

public sealed class SubjectUserIdModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext.HttpContext.Items.TryGetValue(GuardianContextKeys.SubjectUserId, out var value)
            && value is Guid subjectUserId)
        {
            bindingContext.Result = ModelBindingResult.Success(subjectUserId);
            return Task.CompletedTask;
        }

        /*
         * Absent means the action carries [FromSubject] but not [AcceptsSubject]. That is a wiring
         * bug, and binding Guid.Empty instead would reproduce the silent write this whole scheme
         * exists to remove.
         */
        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
            "No validated subject on this request. [FromSubject] requires [AcceptsSubject] on the same action.");
        bindingContext.Result = ModelBindingResult.Failed();
        return Task.CompletedTask;
    }
}
