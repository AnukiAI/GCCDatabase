using System.Linq.Expressions;

namespace GCCWebApp.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression);
        Task<T?> FindFirstOrDefaultAsync(Expression<Func<T, bool>> expression);
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        Task<T> UpdateAsync(T entity);
        Task RemoveAsync(T entity);
        Task RemoveRangeAsync(IEnumerable<T> entities);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> expression);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> expression);

        // Queryable for complex queries
        IQueryable<T> Query();
    }
}