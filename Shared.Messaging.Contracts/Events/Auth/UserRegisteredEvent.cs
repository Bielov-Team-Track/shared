namespace Shared.Messaging.Contracts.Events.Auth;

public class UserRegisteredEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}
