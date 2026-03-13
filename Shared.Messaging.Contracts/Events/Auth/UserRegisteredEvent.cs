using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Auth;

public class UserRegisteredEvent : IEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public AgeTier AgeTier { get; set; }

    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
