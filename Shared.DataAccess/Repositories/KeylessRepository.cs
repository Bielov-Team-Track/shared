using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.DataAccess.Repositories.Interfaces;

namespace Shared.DataAccess.Repositories;

public class KeylessRepository<T> : IKeylessRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public KeylessRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual IQueryable<T> Query() => _dbSet.AsQueryable();
    public virtual IQueryable<T> QueryNoTracking() => _dbSet.AsNoTracking();

    public virtual async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.Where(predicate).ToListAsync();

    public virtual void Add(T entity) => _dbSet.Add(entity);
    public virtual void AddRange(IEnumerable<T> entities) => _dbSet.AddRange(entities);
    public virtual void Update(T entity) => _dbSet.Update(entity);
    public virtual void Delete(T entity) => _dbSet.Remove(entity);

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => _dbSet.AnyAsync(predicate);

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    public virtual async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try { var result = await operation(); await transaction.CommitAsync(); return result; }
        catch { await transaction.RollbackAsync(); throw; }
    }

    public virtual async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try { await operation(); await transaction.CommitAsync(); }
        catch { await transaction.RollbackAsync(); throw; }
    }
}
