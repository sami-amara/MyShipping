using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DataAccessLayer.Contracts;
using DataAccessLayer.DbContext;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using Domains;

namespace DataAccessLayer.Repositories
{
    public class SecurityAuditLogRepository : ISecurityAuditLogRepository
    {
        private readonly ShippingContext _context;

        public SecurityAuditLogRepository(ShippingContext context)
        {
            _context = context;
        }
        // public async Task<List<TResult>> GetList<TResult>(
        //Expression<Func<Log, bool>>? filter = null,
        //Expression<Func<Log, TResult>>? selector = null,
        //Expression<Func<Log, object>>? orderBy = null,
        //bool isDescending = false)
        // {
        //     IQueryable<Log> query = _context.Set<Log>();

        //     if (filter != null)
        //         query = query.Where(filter);

        //     if (orderBy != null)
        //         query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        //     if (selector != null)
        //         return await query.Select(selector).ToListAsync();

        //     return await query.Cast<TResult>().ToListAsync();
        // }
        public async Task<List<TResult>> GetList<TResult>(
            Expression<Func<SecurityAuditLog, bool>>? filter = null,
            Expression<Func<SecurityAuditLog, TResult>>? selector = null,
            Expression<Func<SecurityAuditLog, object>>? orderBy = null,
            bool isDescending = false)
        {
            IQueryable<SecurityAuditLog> query = _context.Set<SecurityAuditLog>();

            if (filter != null)
                query = query.Where(filter);

            if (orderBy != null)
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            if (selector != null)
                return await query.Select(selector).ToListAsync();

            return await query.Cast<TResult>().ToListAsync();
        }
    }
}