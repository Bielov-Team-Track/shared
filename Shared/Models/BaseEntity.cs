namespace Shared.Models;

public abstract class BaseEntity<TKey> : IEntity<TKey>, IAuditable, ISoftDelete
{
    public TKey Id { get; set; } = default!;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}
