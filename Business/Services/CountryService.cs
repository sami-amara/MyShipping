


using Business.Contracts;
using Business.Services;
using DataAccessLayer.Contracts; // Add this line
using DataAccessLayer.Repositories;
using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Business.DTOS;
using AutoMapper;


namespace Business.Services
{
    public class CountryService : BaseService<TbCountry, CountryDto>, ICountry
    {
        public CountryService(IGenericRepository<TbCountry> repository, IMapper mapper, IUserService userService) : base(repository, mapper, userService)
        {

        }
    }
}
