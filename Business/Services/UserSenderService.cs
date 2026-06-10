


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
    public class UserSenderService : BaseService<TbUserSender, UserSenderDto>, IUserSender
    {
        private readonly IUnitOfWork _unitOfWork;
        public UserSenderService(IGenericRepository<TbUserSender> repository, IMapper mapper,
            IUserService userService, IUnitOfWork unitOfWork)
            : base(repository, mapper, userService)
        {
            _unitOfWork = unitOfWork;
        }
    }
}
