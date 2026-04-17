namespace Shared.Models;

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}
