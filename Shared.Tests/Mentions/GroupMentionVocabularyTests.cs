using FluentAssertions;
using Shared.Contracts.Enums;
using Shared.Contracts.Mentions;

namespace Shared.Tests.Mentions;

[TestFixture]
[Category("Unit")]
public class GroupMentionVocabularyTests
{
    // Every term and the kind it declares. A term added without a decision about its kind fails
    // here rather than silently inheriting the default and behaving like a position mention.
    private static readonly Dictionary<string, GroupMentionKind> ExpectedKinds = new()
    {
        ["everyone"] = GroupMentionKind.Everyone,
        ["guardians"] = GroupMentionKind.Guardians,
        ["setters"] = GroupMentionKind.Position,
        ["outsides"] = GroupMentionKind.Position,
        ["opposites"] = GroupMentionKind.Position,
        ["middles"] = GroupMentionKind.Position,
        ["liberos"] = GroupMentionKind.Position,
    };

    [Test]
    public void All_EveryTerm_DeclaresItsKindExplicitly()
    {
        // Act
        var declared = GroupMentionVocabulary.All.ToDictionary(term => term.Token, term => term.Kind);

        // Assert
        declared.Should().BeEquivalentTo(ExpectedKinds);
    }

    [Test]
    public void TryResolveToken_Guardians_ResolvesToTheGuardiansKind()
    {
        // Act
        var resolved = GroupMentionVocabulary.TryResolveToken("guardians", out var term);

        // Assert
        resolved.Should().BeTrue();
        term.Kind.Should().Be(GroupMentionKind.Guardians);
        term.Position.Should().BeNull();
        term.RequiresStaffRole.Should().BeTrue("broadcasting to every parent is the same blast as @everyone");
    }

    [Test]
    public void TryResolveToken_Everyone_StillResolvesToEveryone()
    {
        // Act
        var resolved = GroupMentionVocabulary.TryResolveToken("everyone", out var term);

        // Assert
        resolved.Should().BeTrue();
        term.Kind.Should().Be(GroupMentionKind.Everyone);
        term.Position.Should().BeNull();
        term.RequiresStaffRole.Should().BeTrue();
    }

    [Test]
    public void All_NoTwoTermsShareAToken()
    {
        // Act
        var tokens = GroupMentionVocabulary.All
            .SelectMany(term => term.Aliases.Append(term.Token))
            .ToList();

        // Assert
        tokens.Should().OnlyHaveUniqueItems(
            "a duplicate token throws in the static initialiser and takes every service down at startup");
    }

    [Test]
    public void ForPosition_IsUnaffectedByTheNewTerm()
    {
        // Act & Assert
        foreach (var position in Enum.GetValues<VolleyballPosition>())
        {
            var term = GroupMentionVocabulary.ForPosition(position);

            term.Position.Should().Be(position);
            term.Kind.Should().Be(GroupMentionKind.Position);
        }
    }
}
