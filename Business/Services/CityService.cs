using AutoMapper;
using Business.Contracts;
using Business.DTOS;
using Business.Services;
using DataAccessLayer.Contracts; // Add this line
using DataAccessLayer.Repositories;
using Domains;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;



namespace Business.Services
{
    public class CityService : BaseService<TbCity, CityDto>, ICity
    {
        IViewRepository<VwCities> _Vwcities;
        IMapper _mapper;
        public CityService(IGenericRepository<TbCity> repo, IMapper mapper,
            IUserService userService, IViewRepository<VwCities> vwcities) : base(repo, mapper, userService)
        {
            _Vwcities = vwcities;
            _mapper = mapper;
        }

        public async Task<List<CityDto>> GetAllCitites()
        {
            var cities = _Vwcities.GetList(a => a.CurrentState == 1).ToList();
            
            return _mapper.Map<List<VwCities>, List<CityDto>>(cities);
        }

        public async Task<List<CityDto>> GetByCountryId(Guid countryId)
        {
            var cities = _Vwcities.GetList(a => a.CurrentState == 1 && a.CountryId == countryId).ToList();
            return _mapper.Map<List<VwCities>, List<CityDto>>(cities);
        }
    }

   

    
}
