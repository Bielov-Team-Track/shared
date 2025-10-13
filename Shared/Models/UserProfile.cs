namespace Shared.Models;

public class UserProfile : BaseEntity
{
    public string? Name { get; set; }

    public string? Surname { get; set; }

    public string? ImageUrl { get; set; }

    public string? Email { get; set; }

    // UserId is the same as Id (inherited from BaseEntity) - it's the user's ID from auth service
    public Guid UserId
    {
        get => Id;
        set => Id = value;
    }
}
