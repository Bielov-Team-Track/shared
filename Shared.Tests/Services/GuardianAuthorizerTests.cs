using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Services;

namespace Shared.Tests.Services;

[TestFixture]
[Category("Unit")]
public class GuardianAuthorizerTests
{
    private const string CacheAuthSource = "cache";
    private const string DatabaseAuthSource = "database";

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();

    private IGuardianCacheService _cache = null!;
    private IActionRiskClassifier _riskClassifier = null!;
    private IGuardianAccessSource _source = null!;
    private GuardianAuthorizer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = Substitute.For<IGuardianCacheService>();
        _riskClassifier = Substitute.For<IActionRiskClassifier>();
        _source = Substitute.For<IGuardianAccessSource>();

        _riskClassifier.GetRiskLevel(Arg.Any<HttpRequest>()).Returns(ActionRiskLevel.Low);
        _cache.HasAccessWithCacheAsync(ActorId, SubjectId).Returns((true, CacheAuthSource));
        _cache.HasAccessFromDbAsync(ActorId, SubjectId).Returns((true, DatabaseAuthSource));

        _sut = new GuardianAuthorizer(_cache, _riskClassifier, _source,
            Substitute.For<ILogger<GuardianAuthorizer>>());
    }

    private static HttpRequest Request(string method = "GET")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        return context.Request;
    }

    private void GivenRisk(ActionRiskLevel level) =>
        _riskClassifier.GetRiskLevel(Arg.Any<HttpRequest>()).Returns(level);

    private void GivenNoLink() =>
        _cache.HasAccessWithCacheAsync(ActorId, SubjectId).Returns((false, CacheAuthSource));

    private void GivenRemovalNotice() =>
        _cache.GetRemovalNoticeStatusAsync(ActorId, SubjectId)
            .Returns(new GuardianRemovalStatus { IsUnderRemovalNotice = true });

    private void GivenSnapshot(GuardianAccessSnapshot? snapshot) =>
        _source.CheckAsync(ActorId, SubjectId, Arg.Any<IReadOnlyCollection<ConsentType>?>(),
            Arg.Any<CancellationToken>()).Returns(snapshot);

    private static GuardianAccessSnapshot Snapshot(
        GuardianPermission permissions,
        bool hasAccess = true,
        bool isUnderRemovalNotice = false,
        params ConsentType[] consents) =>
        new(hasAccess, isUnderRemovalNotice, permissions, consents.ToHashSet());

    private Func<Task<GuardianAuthorization>> Authorizing(
        GuardianPermission permission = GuardianPermission.None,
        ConsentType? consent = null,
        string method = "GET") =>
        () => _sut.AuthorizeAsync(ActorId, SubjectId, permission, consent, Request(method));

    [Test]
    public async Task AuthorizeAsync_HighRiskRequest_ChecksTheDatabaseNotTheCache()
    {
        // Arrange
        GivenRisk(ActionRiskLevel.High);

        // Act
        var result = await _sut.AuthorizeAsync(ActorId, SubjectId, GuardianPermission.None, null, Request());

        // Assert
        result.AuthorizationSource.Should().Be(DatabaseAuthSource);
        await _cache.Received(1).HasAccessFromDbAsync(ActorId, SubjectId);
        await _cache.DidNotReceive().HasAccessWithCacheAsync(ActorId, SubjectId);
    }

    [Test]
    public async Task AuthorizeAsync_LowRiskRequest_UsesTheCache()
    {
        // Arrange
        GivenRisk(ActionRiskLevel.Low);

        // Act
        var result = await _sut.AuthorizeAsync(ActorId, SubjectId, GuardianPermission.None, null, Request());

        // Assert
        result.AuthorizationSource.Should().Be(CacheAuthSource);
        await _cache.Received(1).HasAccessWithCacheAsync(ActorId, SubjectId);
        await _cache.DidNotReceive().HasAccessFromDbAsync(ActorId, SubjectId);
    }

    [Test]
    public async Task AuthorizeAsync_NoLink_ThrowsForbiddenWithNoErrorCode()
    {
        // Arrange
        GivenNoLink();

        // Act & Assert
        (await Authorizing().Should().ThrowAsync<ForbiddenException>())
            .Which.Should().Match<ForbiddenException>(e =>
                e.Message == "Access denied" && e.ErrorCode == ErrorCodeEnum.Forbidden);
    }

    [Test]
    public async Task AuthorizeAsync_UnderRemovalNoticeAndWriteMethod_Throws()
    {
        // Arrange
        GivenRemovalNotice();

        // Act & Assert
        foreach (var method in new[] { "POST", "PUT", "PATCH", "DELETE" })
            await Authorizing(method: method).Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task AuthorizeAsync_UnderRemovalNoticeAndGet_Allowed()
    {
        // Arrange
        GivenRemovalNotice();

        // Act & Assert
        await Authorizing(method: "GET").Should().NotThrowAsync();
    }

    [Test]
    public async Task AuthorizeAsync_NoPermissionAndNoConsentRequired_DoesNotCallTheSource()
    {
        // Arrange
        // Act
        await _sut.AuthorizeAsync(ActorId, SubjectId, GuardianPermission.None, null, Request());

        // Assert
        await _source.DidNotReceiveWithAnyArgs().CheckAsync(default, default, default, default);
    }

    [Test]
    public async Task AuthorizeAsync_SourceReturnsNull_ThrowsForbidden()
    {
        // Arrange
        GivenSnapshot(null);

        // Act & Assert
        (await Authorizing(consent: ConsentType.EventParticipation).Should().ThrowAsync<ForbiddenException>())
            .Which.Should().Match<ForbiddenException>(e =>
                e.Message == "Access denied" && e.ErrorCode == ErrorCodeEnum.Forbidden);
    }

    [TestCase(ActionRiskLevel.Low)]
    [TestCase(ActionRiskLevel.High)]
    public async Task AuthorizeAsync_SourceReturnsNull_DoesNotFallBackToTheCacheAnswer(ActionRiskLevel risk)
    {
        // Arrange
        GivenRisk(risk);
        GivenSnapshot(null);

        // Act & Assert
        await Authorizing(GuardianPermission.Pay).Should().ThrowAsync<ForbiddenException>();
    }

    [Test]
    public async Task AuthorizeAsync_SourceDeniesAccess_ThrowsGuardianAccessDenied()
    {
        // Arrange
        GivenSnapshot(Snapshot(GuardianPermission.None, hasAccess: false));

        // Act & Assert
        (await Authorizing(GuardianPermission.Pay).Should().ThrowAsync<ForbiddenException>())
            .Which.ErrorCode.Should().Be(ErrorCodeEnum.GuardianAccessDenied);
    }

    [Test]
    public async Task AuthorizeAsync_SourceReportsRemovalNotice_ThrowsGuardianAccessDenied()
    {
        // Arrange
        GivenSnapshot(Snapshot(GuardianPermission.Admin, isUnderRemovalNotice: true));

        // Act & Assert
        (await Authorizing(GuardianPermission.Admin).Should().ThrowAsync<ForbiddenException>())
            .Which.ErrorCode.Should().Be(ErrorCodeEnum.GuardianAccessDenied);
    }

    [Test]
    public async Task AuthorizeAsync_PermissionBitMissing_ThrowsGuardianAccessDenied()
    {
        // Arrange
        GivenSnapshot(Snapshot(GuardianPermission.View, consents: ConsentType.EventParticipation));

        // Act & Assert
        (await Authorizing(GuardianPermission.Pay, ConsentType.EventParticipation)
                .Should().ThrowAsync<ForbiddenException>())
            .Which.Should().Match<ForbiddenException>(e =>
                e.Message == "Guardian lacks Pay permission" &&
                e.ErrorCode == ErrorCodeEnum.GuardianAccessDenied);
    }

    [Test]
    public async Task AuthorizeAsync_ConsentMissing_ThrowsConsentNotFound()
    {
        // Arrange
        GivenSnapshot(Snapshot(GuardianPermission.Pay));

        // Act & Assert
        (await Authorizing(GuardianPermission.Pay, ConsentType.PaymentProcessing)
                .Should().ThrowAsync<ForbiddenException>())
            .Which.Should().Match<ForbiddenException>(e =>
                e.Message == "Minor has not consented to PaymentProcessing" &&
                e.ErrorCode == ErrorCodeEnum.ConsentNotFound);
    }

    [Test]
    public async Task AuthorizeAsync_ConsentRequired_AsksTheSourceForThatConsentOnly()
    {
        // Arrange
        GivenSnapshot(Snapshot(GuardianPermission.Pay, consents: ConsentType.PaymentProcessing));

        // Act
        await _sut.AuthorizeAsync(ActorId, SubjectId, GuardianPermission.Pay,
            ConsentType.PaymentProcessing, Request());

        // Assert
        await _source.Received(1).CheckAsync(ActorId, SubjectId,
            Arg.Is<IReadOnlyCollection<ConsentType>?>(c =>
                c != null && c.Count == 1 && c.Contains(ConsentType.PaymentProcessing)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AuthorizeAsync_Success_ReturnsTheAuthorizationSourceFromTheCacheCall()
    {
        // Arrange
        GivenSnapshot(Snapshot(GuardianPermission.Pay | GuardianPermission.RSVP,
            consents: ConsentType.PaymentProcessing));

        // Act
        var result = await _sut.AuthorizeAsync(ActorId, SubjectId, GuardianPermission.Pay,
            ConsentType.PaymentProcessing, Request());

        // Assert
        result.AuthorizationSource.Should().Be(CacheAuthSource);
        result.Permissions.Should().Be(GuardianPermission.Pay | GuardianPermission.RSVP);
    }
}
