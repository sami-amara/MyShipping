using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts
{
    public interface IBaseService<T,DTO>
    {
        Task<List<DTO>> GetAll();
        Task<DTO> GetById(Guid id);
        Task<(bool Success, Guid Id)> Add(DTO entity);
        Task<bool> UpdateAsync(DTO entity);
        Task<bool> ChangeStatus(Guid id, int status = 1);
        Task<bool> Delete(Guid id);
    }
}
