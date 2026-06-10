using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.DTOS;
using DataAccessLayer.Contracts;
using Domains;

namespace Business.Contracts
{
    public interface ICity : IBaseService<TbCity, CityDto>
    {
        
        Task<List<CityDto>> GetAllCitites();
        
        Task<List<CityDto>> GetByCountryId(Guid countryId);
    }
}
