using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DataAccessLayer.Models;
using Domains;

namespace DataAccessLayer.Contracts
{
    public interface ISecurityAuditLogRepository
    {

        Task<List<TResult>> GetList<TResult>(
            Expression<Func<SecurityAuditLog, bool>>? filter = null,
            Expression<Func<SecurityAuditLog, TResult>>? selector = null,
            Expression<Func<SecurityAuditLog, object>>? orderBy = null,
            bool isDescending = false);
    }
}
