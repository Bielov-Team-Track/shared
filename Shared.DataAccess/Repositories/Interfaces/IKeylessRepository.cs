using System.Linq.Expressions;

namespace Shared.DataAccess.Repositories.Interfaces;

// "Keyless" is a slight misnomer — these entities DO have keys, just not a single-Guid-named-Id one.
// The contract omits key-based lookups and exposes only query + mutation primitives.
public interface IKeylessRepository<T> where T : class
{
    IQueryable<T> Query();
    IQueryable<T> QueryNoTracking();
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task SaveChangesAsync();

    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
    Task ExecuteInTransactionAsync(Func<Task> operation);
}
