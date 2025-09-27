using System.Linq.Expressions;

namespace Shared.DataAccess.Repositories.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    IQueryable<T> Query();

    // Transaction support
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
    Task ExecuteInTransactionAsync(Func<Task> operation);
}