using DataAccessLayer.Contracts;
using DataAccessLayer.DbContext;
using DataAccessLayer.Exceptions;
using DataAccessLayer.Model;
using Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;



namespace DataAccessLayer.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseTable
    {
        private readonly ShippingContext _context;
        private readonly DbSet<T> _dbSet;
        private readonly ILogger<GenericRepository<T>> _logger;
        public GenericRepository(ShippingContext context, ILogger<GenericRepository<T>> log)
        {
            _context = context;
            _dbSet = _context.Set<T>();
            _logger = log;
        }

        public async Task<List<T>> GetAll()
        {
            try
            {
                return await _dbSet.Where(a => !a.IsDeleted).ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to get all data", _logger);
            }
        }
        
        public async Task<T> GetById(Guid id)
        {
            try
            {
                var result = await _dbSet.Where(a => a.Id == id).AsNoTracking().FirstOrDefaultAsync().ConfigureAwait(false);
                if (result == null)
                {
                    throw new DataAccessExceptions(new Exception("Entity not found"), "Error Occured While Getting Data By Id", _logger);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Error Occured While Getting Data By Id", _logger);
            }
        }

        public async Task<(bool Success, Guid Id)> Add(T entity)
        {
            try
            {
                entity.CreatedDate = DateTime.UtcNow;
                entity.UpdatedDate = null;

                await _dbSet.AddAsync(entity).ConfigureAwait(false);
                await _context.SaveChangesAsync().ConfigureAwait(false);

                return (true, entity.Id);
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to add", _logger);
            }
        }

        public async Task<bool> Update(T entity)
        {
            var dbData = await _dbSet.FirstOrDefaultAsync(a => a.Id == entity.Id).ConfigureAwait(false);
            if (dbData == null) return false;



            entity.CreatedDate = dbData.CreatedDate;
            entity.CreatedBy = dbData.CreatedBy;
            entity.UpdatedDate = DateTime.UtcNow;
            entity.CurrentState = dbData.CurrentState;

            _context.Entry(dbData).CurrentValues.SetValues(entity);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            return true;
        }

      
        public async Task<bool> UpdateFields(Guid id, Action<T> updateAction)
        {
            try
            {
                var dbData = await _dbSet.FirstOrDefaultAsync(d => d.Id == id).ConfigureAwait(false);
                if (dbData == null)
                {

                    throw new DataAccessExceptions(new Exception("Entity not found"), "Error Occured While Updating Data", _logger);
                }
                // Apply the update action to the entity

                updateAction(dbData);
                _context.Entry(dbData).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                await _context.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Error Uccured While Updatin Data", _logger);
            }
        }
       


        public async Task<bool> Delete(Guid id)
        {
            try
            {
                var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id).ConfigureAwait(false);
                if (entity == null)
                {
                    return false;
                }

                // Soft delete: set IsDeleted flag instead of removing from database
                entity.IsDeleted = true;
                entity.DeletedDate = DateTime.UtcNow;
                // Note: DeletedBy should be set by the caller/service layer with user context

                await _context.SaveChangesAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to delete", _logger);
            }

        }

        public async Task<bool> Delete(Guid id, Guid userId)
        {
            try
            {
                var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id).ConfigureAwait(false);
                if (entity == null)
                {
                    return false;
                }

                // Soft delete with user tracking
                entity.IsDeleted = true;
                entity.DeletedDate = DateTime.UtcNow;
                entity.DeletedBy = userId;

                await _context.SaveChangesAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to delete", _logger);
            }
        }

       
        public async Task<bool> ChangeStatus(Guid id, Guid userId, int status = (int)Domains.EntityState.Active)
        {
            try
            {
                var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id).ConfigureAwait(false);
                if (entity == null)
                {
                    return false;
                }

                entity.CurrentState = status;
                entity.UpdatedBy = userId;  
                entity.UpdatedDate = DateTime.UtcNow;  // ✅ UTC time

                await _context.SaveChangesAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to change status", _logger);
            }
           
        }
       


        public async Task<T> GetFirstOrDefault(Expression<Func<T, bool>> filter)
        {
            try
            {

                var result = await _dbSet.Where(filter).AsNoTracking().FirstOrDefaultAsync().ConfigureAwait(false);
                if (result == null)
                {
                    throw new DataAccessExceptions(new Exception("Entity not found"), "Failed to Get First", _logger);
                }
                return result;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new DataAccessExceptions(ex, "Failed to Get First", _logger);
            }
        }
       
        public async Task<List<T>> GetList(Expression<Func<T, bool>> filter)
        {
            try
            {
                return await _dbSet.Where(filter).AsNoTracking().ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new DataAccessExceptions(ex, "Failed to Get List", _logger);
            }
        }



        // Overload: include, ordering (by selector) and descending flag
        public async Task<List<T>> GetList(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<T, object>> orderBy = null,
            bool isDescending = false,
            params Expression<Func<T, object>>[] includes)
        {
            try
            {
                IQueryable<T> query = _dbSet.AsNoTracking();

                if (filter != null)
                    query = query.Where(filter);

                if (includes != null && includes.Length > 0)
                {
                    foreach (var include in includes)
                        query = query.Include(include);
                }

                if (orderBy != null)
                {
                    query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
                }

                return await query.ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to Get List (with options)", _logger);
            }
        }

        // Generic projection overload: selector projects T to TResult
        public async Task<List<TResult>> GetList<TResult>(
            Expression<Func<T, bool>> filter = null,
            Expression<Func<T, TResult>> selector = null,
            Expression<Func<T, object>> orderBy = null,
            bool isDescending = false,
            params Expression<Func<T, object>>[] includes)
        {
            try
            {
                IQueryable<T> query = _dbSet.AsNoTracking();

                if (filter != null)
                    query = query.Where(filter);

                if (includes != null && includes.Length > 0)
                {
                    foreach (var include in includes)
                        query = query.Include(include);
                }

                if (orderBy != null)
                {
                    query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
                }

                if (selector != null)
                {
                    return await query.Select(selector).ToListAsync().ConfigureAwait(false);
                }

                // If no selector provided, attempt to cast T -> TResult (only safe if TResult == T)
                if (typeof(TResult) == typeof(T))
                {
                    var list = await query.ToListAsync().ConfigureAwait(false);
                    return list.Cast<TResult>().ToList();
                }

                throw new DataAccessExceptions(new InvalidOperationException("Selector is required when TResult differs from T"), "Failed to Get List<TResult>", _logger);
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to Get List<TResult> (with options)", _logger);
            }

        }

        public async Task<PagedResult<TResult>> GetPagedList<TResult>(
        Expression<Func<T, bool>> filter = null,
        Expression<Func<T, TResult>> selector = null,
        Expression<Func<T, object>> orderBy = null,
        bool isDescending = false,
        int page = 1,
        int pageSize = 10,
        params Expression<Func<T, object>>[] includes)
        {

            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                // Start with a lightweight query used to compute total count.
                IQueryable<T> baseQuery = _dbSet.AsNoTracking();

                if (filter != null)
                    baseQuery = baseQuery.Where(filter);

                // Compute total count on the base filtered query WITHOUT applying Includes.
                // Includes cause extra JOINs and can slow down COUNT significantly.
                var total = await baseQuery.CountAsync().ConfigureAwait(false);

                // Build the query we will page over.
                // Use the same baseQuery (no includes). EF will generate JOINs for ordering
                // or projections that reference navigation properties as needed.
                IQueryable<T> pagedQuery = baseQuery;

                // Ordering (EF will add JOINs if orderBy references navigation properties)
                if (orderBy != null)
                {
                    pagedQuery = isDescending ? pagedQuery.OrderByDescending(orderBy) : pagedQuery.OrderBy(orderBy);
                }

                // Paging
                var skip = (page - 1) * pageSize;
                pagedQuery = pagedQuery.Skip(skip).Take(pageSize);

                // Projection / materialization.
                // If the caller provided a selector (projection), we do NOT apply Includes:
                // projecting directly is more efficient and doesn't require Include().
                List<TResult> items;
                if (selector != null)
                {
                    items = await pagedQuery.Select(selector).ToListAsync().ConfigureAwait(false);
                }
                else if (typeof(TResult) == typeof(T))
                {
                    // If result type is the entity type, then Includes are relevant.
                    if (includes != null && includes.Length > 0)
                    {
                        foreach (var include in includes)
                            pagedQuery = pagedQuery.Include(include);
                    }

                    var list = await pagedQuery.ToListAsync().ConfigureAwait(false);
                    items = list.Cast<TResult>().ToList();
                }
                else
                {
                    throw new InvalidOperationException("Selector must be provided when TResult differs from T");
                }

                return new PagedResult<TResult>
                {
                    Items = items,
                    PageNumber = page,
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to Get Paged List", _logger);
            }
        }
    }
}






















































































































