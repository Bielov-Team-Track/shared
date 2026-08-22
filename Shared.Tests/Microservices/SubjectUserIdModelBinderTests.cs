using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.Microservices.Guardian;
using Shared.Services;

namespace Shared.Tests.Microservices;

[TestFixture]
[Category("Unit")]
public class SubjectUserIdModelBinderTests
{
    private const string ModelName = "subjectUserId";

    private SubjectUserIdModelBinder _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new SubjectUserIdModelBinder();

    private static DefaultModelBindingContext BindingContextFor(Guid? subjectUserId)
    {
        var httpContext = new DefaultHttpContext();
        if (subjectUserId is { } id)
            httpContext.Items[GuardianContextKeys.SubjectUserId] = id;

        return new DefaultModelBindingContext
        {
            ActionContext = new ActionContext { HttpContext = httpContext },
            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(Guid)),
            ModelName = ModelName,
            ModelState = new ModelStateDictionary()
        };
    }

    [Test]
    public async Task ItemPresent_BindsTheGuid()
    {
        // Arrange
        var subjectUserId = Guid.NewGuid();
        var context = BindingContextFor(subjectUserId);

        // Act
        await _sut.BindModelAsync(context);

        // Assert
        context.Result.IsModelSet.Should().BeTrue();
        context.Result.Model.Should().Be(subjectUserId);
    }

    /// <summary>
    /// Absent means the action carries [FromSubject] without [AcceptsSubject]. That is a wiring
    /// bug, and a silent Guid.Empty is how it would reach production unnoticed.
    /// </summary>
    [Test]
    public async Task ItemAbsent_FailsWithAModelError()
    {
        // Arrange
        var context = BindingContextFor(null);

        // Act
        await _sut.BindModelAsync(context);

        // Assert
        context.Result.IsModelSet.Should().BeFalse();
        context.ModelState[ModelName]!.Errors.Should().ContainSingle();
    }
}
