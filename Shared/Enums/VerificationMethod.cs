namespace Shared.Enums;

public enum VerificationMethod
{
    EmailPlus,
    TextPlus,
    SmsPlusKnowledgeBased,
    SmsPlusCallback,
    KnowledgeBased,
    CreditCard,

    // The minor themself affirming, not an adult authenticating. Every other member records a
    // guardian proving who they are; here the guardian proves nothing and the child answers. Writing
    // EmailPlus against a guardian who never saw a consent screen would corrupt the one record that
    // exists to show who actually agreed to what.
    MinorAffirmation
}
