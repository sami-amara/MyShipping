using DataAccessLayer.DbContext;
using DataAccessLayer.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Exceptions;
using Domains;
using System.Linq.Expressions;

namespace DataAccessLayer.Repositories
{
    public class ViewRepository<T> : IViewRepository<T> where T : class
    {
        private readonly ShippingContext _context;
        private readonly DbSet<T> _dbSet;
        private readonly ILogger<ViewRepository<T>> _logger;
        public ViewRepository(ShippingContext context, ILogger<ViewRepository<T>> log)
        {
            _context = context;
            _dbSet = _context.Set<T>();
            _logger = log;
        }

        public List<T> GetAll()
        {
            try
            {
                return _dbSet.AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to get all data", _logger);
            }
        }
        public T GetById(Guid id)
        {
            try
            {
                return _dbSet.AsNoTracking().FirstOrDefault()!;
            }
            catch (Exception ex)
            {
                throw new DataAccessExceptions(ex, "Failed to get by id", _logger);
            }
        }


        public T GetFirstOrDefault(Expression<Func<T, bool>> filter)
        {
            try
            {

                return _dbSet.Where(filter).AsNoTracking().FirstOrDefault(filter);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new DataAccessExceptions(ex, "Failed to Get First", _logger);
            }
        }

        public List<T> GetList(Expression<Func<T, bool>> filter)
        {
            try
            {
                return _dbSet.Where(filter).AsNoTracking().ToList();
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                throw new DataAccessExceptions(ex, "Failed to Get List", _logger);
            }
        }
    }
}
