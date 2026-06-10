using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Contracts
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        IGenericRepository<T> Repository<T>() where T : BaseTable;   // generic accessor
        Task BeginTransactionAsync();
        Task CommitAsync();
        
        Task RollbackAsync();
        Task<int> SaveChangesAsync();
    }
}
