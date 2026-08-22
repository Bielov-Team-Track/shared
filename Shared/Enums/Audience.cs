namespace Shared.Enums;

/// <summary>
/// A named set of people in a context. The only way a feature should ever enumerate people
/// (spec §6.10). Members is the default everywhere: a caller that does not ask gets the direct
/// rows, which is what every caller written before this enum existed already meant.
/// </summary>
public enum Audience
{
    Members = 0,
    Players = 1,
    Staff = 2,
    Guardians = 3,
    Everyone = 4,
}
