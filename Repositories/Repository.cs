using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using GCCWebApp.Data;

namespace GCCWebApp.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> expression)
        {
            return await _dbSet.Where(expression).ToListAsync();
        }

        public virtual async Task<T?> FindFirstOrDefaultAsync(Expression<Func<T, bool>> expression)
        {
            return await _dbSet.FirstOrDefaultAsync(expression);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            var result = await _dbSet.AddAsync(entity);
            return result.Entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        public virtual Task<T> UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return Task.FromResult(entity);
        }

        public virtual Task RemoveAsync(T entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public virtual Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            return Task.CompletedTask;
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            return entity != null;
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> expression)
        {
            return await _dbSet.AnyAsync(expression);
        }

        public virtual async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> expression)
        {
            return await _dbSet.CountAsync(expression);
        }

        public virtual IQueryable<T> Query()
        {
            return _dbSet.AsQueryable();
        }
    }
}