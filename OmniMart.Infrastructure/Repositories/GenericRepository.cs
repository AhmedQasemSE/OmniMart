using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Infrastructure.Data;
using System.Linq.Expressions;

namespace OmniMart.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        internal DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes) 
        {
            return await GetAsync (predicate, CancellationToken.None,includes);
        }
        public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellation, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var include in includes) 
            {
                query = query.Include(include);
            }
            return await query.FirstOrDefaultAsync(predicate, cancellation);
        }

        public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            return await GetAllAsync( CancellationToken.None, includes);
        }
        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellation, params Expression<Func<T, object>>[] includes )
        {
            IQueryable<T> query = _dbSet;
            foreach (var include in includes) 
            {
                query = query.Include(include);
            }
            return await query.ToListAsync(cancellation);
        }
        public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }

        public void SetOriginalRowVersion(T entity, byte[] rowVersion)
        {
            _context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }
    }
}