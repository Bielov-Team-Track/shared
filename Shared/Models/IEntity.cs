namespace Shared.Models;

public interface IEntity<out TKey>
{
    TKey Id { get; }
}
