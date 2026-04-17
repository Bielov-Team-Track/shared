using System.Linq.Expressions;
using Shared.Models;

namespace Shared.DataAccess.Repositories.Interfaces;

public interface IRepository<T, TKey> where T : class, IEntity<TKey>
{
    Task<T?> GetByIdAsync(TKey id);
    Task<T?> GetByIdAsync(TKey id, params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Update(T entity);
    void Delete(T entity);
    Task DeleteByIdAsync(TKey id);
    Task<bool> ExistsAsync(TKey id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> Query();
    IQueryable<T> QueryNoTracking();
    Task SaveChangesAsync();

    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
    Task ExecuteInTransactionAsync(Func<Task> operation);
}

// Convenience alias — 95% of entities are keyed by Guid
public interface IRepository<T> : IRepository<T, Guid> where T : class, IEntity<Guid>
{
}
