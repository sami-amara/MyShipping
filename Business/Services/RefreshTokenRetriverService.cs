using AutoMapper;
using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Contracts;
using Domains;
using System;
using System.Threading.Tasks;

namespace Business.Services
{
    public class RefreshTokenRetriverService : IRefreshTokenRetriver
    {
        private readonly IGenericRepository<TbRefreshToken> _repository;
        private readonly IMapper _mapper;

        public RefreshTokenRetriverService(IGenericRepository<TbRefreshToken> repository, IMapper mapper)
        {
            _mapper = mapper;
            _repository = repository;
        }

        public async Task<RefreshTokenDto> GetByToken(string token)
        {
            var refreshToken = await Task.Run(() => _repository.GetFirstOrDefault(r => r.Token == token));
            if (refreshToken == null)
            {
                throw new Exception("Invalid refresh token");
            }
            return _mapper.Map<TbRefreshToken, RefreshTokenDto>(refreshToken);
        }
    }
}
