using AutoMapper;
using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Contracts;
using Domains;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services
{
    public class BaseService<T, DTO> : IBaseService<T, DTO> where T : BaseTable
    {
        private readonly IGenericRepository<T> _repository;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        public BaseService(IGenericRepository<T> repository, IMapper mapper, IUserService userService)
        {
            _repository = repository;
            _mapper = mapper;
            _userService = userService;
        }
        public BaseService(IUnitOfWork unitOfWork, IMapper mapper, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _repository = unitOfWork.Repository<T>();
            _mapper = mapper;
            _userService = userService;
        }
        public async Task<List<DTO>> GetAll()
        {
            // Await the repository async call directly (do not wrap in Task.Run);
            var list = await _repository.GetAll().ConfigureAwait(false);
            return _mapper.Map<List<T>, List<DTO>>(list);
        }

        public async Task<(bool Success, Guid Id)> Add(DTO entity)
        {
            var dbObject = _mapper.Map<DTO, T>(entity);
            dbObject.CreatedBy = _userService.GetLoggedInUser();
            dbObject.CreatedDate = DateTime.UtcNow;
            dbObject.CurrentState = (int)Domains.EntityState.Active;

            // Use the repository Add and return the generated Id
            var (success, id) = await _repository.Add(dbObject).ConfigureAwait(false);
            return (success, id);
        }

        public async Task<DTO> GetById(Guid id)
        {
            // Await the repository async call directly
            var entity = await _repository.GetById(id).ConfigureAwait(false);
            return _mapper.Map<T, DTO>(entity);
        }

        public async Task<bool> UpdateAsync(DTO entity)
        {
            var objectToAdd = _mapper.Map<DTO, T>(entity);
            objectToAdd.UpdatedDate = DateTime.UtcNow;
            objectToAdd.UpdatedBy = _userService.GetLoggedInUser();
            return await _repository.Update(objectToAdd).ConfigureAwait(false);
        }



        public async Task<bool> ChangeStatus(Guid id, int status = (int)Domains.EntityState.Active)
        {
            return await _repository.ChangeStatus(id, _userService.GetLoggedInUser(), status).ConfigureAwait(false);
        }

        public async Task<bool> Delete(Guid id)
        {
            return await _repository.Delete(id, _userService.GetLoggedInUser()).ConfigureAwait(false);
        }


    }
}




