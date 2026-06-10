using DataAccessLayer.Contracts;
using DataAccessLayer.DbContext;
using Domains;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class UnitOfWork : IUnitOfWork, IDisposable  // ✅ Use IDisposable, not IAsyncDisposable
    {
        private readonly ShippingContext _ctx;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _tx;
        private readonly ILoggerFactory _loggerFactory;
        private bool _disposed = false;  // ✅ Track disposal state

        public UnitOfWork(ShippingContext ctx, ILoggerFactory loggerFactory)
        {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IGenericRepository<T> Repository<T>() where T : BaseTable
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitOfWork));

            return (IGenericRepository<T>)_repositories.GetOrAdd(
                typeof(T),
                _ => new GenericRepository<T>(
                        _ctx,
                        _loggerFactory.CreateLogger<GenericRepository<T>>()));
        }

        public async Task BeginTransactionAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitOfWork));

            if (_tx != null)
                throw new InvalidOperationException("Transaction already started");

            _tx = await _ctx.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitOfWork));

            try
            {
                await _ctx.SaveChangesAsync().ConfigureAwait(false);

                if (_tx != null)
                {
                    await _tx.CommitAsync().ConfigureAwait(false);
                    await _tx.DisposeAsync();  // ✅ Dispose transaction
                    _tx = null;
                }
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            if (_tx != null)
            {
                await _tx.RollbackAsync();
                await _tx.DisposeAsync();  // ✅ Dispose transaction
                _tx = null;
            }
        }

        public Task<int> SaveChangesAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnitOfWork));

            return _ctx.SaveChangesAsync();
        }

        // ✅ CRITICAL CHANGE: Synchronous Dispose (no async)
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // ✅ Only dispose the transaction, NOT the context
                _tx?.Dispose();
                _tx = null;

                // ❌ NEVER DO THIS - DI container manages context lifetime:
                // _ctx?.Dispose();
            }

            _disposed = true;
        }

        // ✅ Optional: Add async disposal support if needed
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            // ✅ Only dispose the transaction, NOT the context
            if (_tx != null)
            {
                await _tx.DisposeAsync();
                _tx = null;
            }

            // ❌ NEVER DO THIS - DI container manages context lifetime:
            // await _ctx.DisposeAsync();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}













//using DataAccessLayer.Contracts;
//using DataAccessLayer.DbContext;
//using Domains;
//using Microsoft.EntityFrameworkCore.Storage;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace DataAccessLayer.Repositories
//{
//    public class UnitOfWork : IUnitOfWork
//    {

//        private readonly ShippingContext _ctx;
//        private readonly ConcurrentDictionary<Type, object> _repositories = new();
//        private IDbContextTransaction? _tx;
//        private readonly ILoggerFactory _loggerFactory;

//        public UnitOfWork(ShippingContext ctx, ILoggerFactory loggerFactory)
//        {
//            _ctx = ctx;
//            _loggerFactory = loggerFactory;
//        }


//        public IGenericRepository<T> Repository<T>() where T : BaseTable
//        {
//            return (IGenericRepository<T>)_repositories.GetOrAdd(
//                typeof(T),
//                _ => new GenericRepository<T>(
//                        _ctx,
//                        _loggerFactory.CreateLogger<GenericRepository<T>>()));
//        }


//        public async Task BeginTransactionAsync()
//            => _tx = await _ctx.Database.BeginTransactionAsync();



//        public async Task CommitAsync()
//        {
//            await _ctx.SaveChangesAsync().ConfigureAwait(false);
//            if (_tx is not null) await _tx.CommitAsync().ConfigureAwait(false);
//        }

//        public async Task RollbackAsync()
//            => await _tx?.RollbackAsync()!;


//        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();

//        public async ValueTask DisposeAsync()
//        {
//            if (_tx is not null) await _tx.DisposeAsync();
//            await _ctx.DisposeAsync();
//        }

//    }
//}
