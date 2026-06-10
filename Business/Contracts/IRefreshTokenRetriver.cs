using Business.DTOS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts
{
    public interface IRefreshTokenRetriver
    {
        //public RefreshTokenDto GetByToken(string token);
        Task<RefreshTokenDto> GetByToken(string token);
    }
}
