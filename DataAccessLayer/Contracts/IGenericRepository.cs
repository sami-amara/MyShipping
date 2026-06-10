using DataAccessLayer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace DataAccessLayer.Contracts
{
    public interface IGenericRepository<T> where T : class
    {
        Task<List<T>> GetAll();
        Task<T> GetById(Guid id);
        Task<(bool Success, Guid Id)> Add(T entity);
        Task<bool> Update(T entity);
        Task<bool> UpdateFields(Guid id, Action<T> updateAction);
        Task<bool> Delete(Guid id);
        Task<bool> Delete(Guid id, Guid userId);
        Task<bool> ChangeStatus(Guid id, Guid userId, int status = 1);

        Task<T> GetFirstOrDefault(Expression<Func<T, bool>> filter);

        Task<List<T>> GetList(Expression<Func<T, bool>> filter);

        Task<List<TResult>> GetList<TResult>(
          Expression<Func<T, bool>>? filter = null,
          Expression<Func<T, TResult>>? selector = null,
          Expression<Func<T, object>>? orderBy = null,
          bool isDescending = false,
          params Expression<Func<T, object>>[] includers);

        Task<PagedResult<TResult>> GetPagedList<TResult>(
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, TResult>>? selector = null,
            Expression<Func<T, object>>? orderBy = null,
            bool isDescending = false,
            int page = 1,
            int pageSize = 10,
            params Expression<Func<T, object>>[] includes);
    }
}
