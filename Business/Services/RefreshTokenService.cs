

using AutoMapper;
using Business.Contracts;
using Business.DTOS;
using Business.Services;
using DataAccessLayer.Contracts; // Add this line
using DataAccessLayer.Repositories;
using Domains;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services
{


    public class RefreshTokenService : BaseService<TbRefreshToken, RefreshTokenDto>, IRefreshToken
    {



        private readonly IGenericRepository<TbRefreshToken> _repository;
        private readonly IMapper _mapper;
        private readonly string _secretKey;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public RefreshTokenService(IGenericRepository<TbRefreshToken> repository, IMapper mapper,
            IUserService userService, IUnitOfWork unitOfWork, IConfiguration configuration)
            : base(repository, mapper, userService)
        {
            _mapper = mapper;
            _repository = repository;
            _secretKey = configuration["SecretKey"] ?? string.Empty;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }
        

        public async Task<bool> RefreshToken(RefreshTokenDto tokenDto)
        {


            // Revoke existing refresh tokens for the user
            var TokenList = await _repository.GetList(r => r.UserId == tokenDto.UserId && r.CurrentState == 1);
            foreach (var userRefreshToken in TokenList)
            {
               await _repository.ChangeStatus(userRefreshToken.Id, Guid.Parse(tokenDto.UserId), 2);
            }

            var dbTokens = _mapper.Map<RefreshTokenDto, TbRefreshToken>(tokenDto);
            await _repository.Add(dbTokens);
            return true;


        }

        // ? NEW: Async version
    


        // Generate a secure refresh token (URL-safe);
        private static string GenerateSecureToken(int size = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(size);
            // Base64 URL safe
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        //// Issue a new refresh token for a user (revokes current active tokens);
        public async Task<RefreshTokenDto> IssueRefreshTokenAsync(string userId, string createdBy = null, int daysValid = 0)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            // ✅ Read refresh token expiration from configuration (default to 7 if not set)
            if (daysValid == 0)
            {
                daysValid = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays");
                if (daysValid == 0)
                {
                    daysValid = 7; // Fallback default
                }
            }

            // Revoke existing active tokens for the user
            var existing = await _repository.GetList(r => r.UserId == userId && r.CurrentState == 1);
            foreach (var tok in existing)
            {
                await _repository.ChangeStatus(tok.Id, Guid.Parse(userId), 2); // 2 = revoked
            }

            var newTokenDto = new RefreshTokenDto
            {
                Token = GenerateSecureToken(64),
                UserId = userId,
                Expires = DateTime.UtcNow.AddDays(daysValid),
                CurrentState = 1 // 1 = active
            };

            var dbToken = _mapper.Map<RefreshTokenDto, TbRefreshToken>(newTokenDto);
            await _repository.Add(dbToken);
            // Map back the stored entity (if repository sets Id);
            var resultDto = _mapper.Map<TbRefreshToken, RefreshTokenDto>(dbToken);
            return resultDto;
        }

        //Validate incoming refresh token and rotate(revoke old, issue new);
        public async Task<RefreshTokenDto> RotateRefreshTokenAsync(string userId, string incomingRefreshToken, string createdBy = null, int daysValid = 0)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(incomingRefreshToken))
                return null;

            // ✅ Read refresh token expiration from configuration (default to 7 if not set)
            if (daysValid == 0)
            {
                daysValid = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays");
                if (daysValid == 0)
                {
                    daysValid = 7; // Fallback default
                }
            }

            // Find the matching token (active);
            var matches = await _repository.GetList(r => r.UserId == userId && r.Token == incomingRefreshToken && r.CurrentState == 1);
            var match = matches?.FirstOrDefault();
            if (match == null)
            {
                // token invalid or already revoked
                return null;
            }

            // Revoke the matched token
            await _repository.ChangeStatus(match.Id, Guid.Parse(userId), 2);
            // Issue a new refresh token
            var newRefresh = await IssueRefreshTokenAsync(userId, createdBy, daysValid);
            return newRefresh;
        }

        // Find user id for a given refresh token (active);
        public async Task<string> GetUserIdByTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) return null;

            var matches = await _repository.GetList(r => r.Token == refreshToken && r.CurrentState == 1);
            var match = matches?.FirstOrDefault();
            return match?.UserId;
        }



         //? ADD THIS: Get all tokens for a user
        public async Task<List<RefreshTokenDto>> GetAllUserTokensAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<RefreshTokenDto>();
            var tokens = await _repository.GetList(r => r.UserId == userId);
            return _mapper.Map<List<TbRefreshToken>, List<RefreshTokenDto>>(tokens);
        }

        //? ADD THIS: Update a specific token(for adding revocation timestamp);
        public async Task UpdateTokenAsync(RefreshTokenDto tokenDto)
        {
            if (tokenDto == null || tokenDto.Id == Guid.Empty)
                return;

            var existingToken = await _repository.GetById(tokenDto.Id);
            if (existingToken != null)
            {
                existingToken.CurrentState = tokenDto.CurrentState;
                existingToken.RevokedAt = tokenDto.RevokedAt;
                existingToken.RevokedReason = tokenDto.RevokedReason;

                await _repository.Update(existingToken);
            }
        }

        // ? ADD THIS: Revoke all active tokens for a user (security incident);
        public async Task RevokeAllUserTokensAsync(string userId, string reason = "Security alert")
        {
            if (string.IsNullOrEmpty(userId))
                return;

            var activeTokens = await _repository.GetList(r => r.UserId == userId && r.CurrentState == 1);
            foreach (var token in activeTokens)
            {
                await _repository.UpdateFields(token.Id, t =>
                {
                    t.CurrentState = 2; // Revoked
                    t.RevokedAt = DateTime.UtcNow;
                    t.RevokedReason = reason;
                });
            }
        }

        // ? ADD THIS: Enhanced version of RotateRefreshTokenAsync with revocation tracking
        public async Task<RefreshTokenDto> RotateRefreshTokenWithTrackingAsync(
            string userId,
            string incomingRefreshToken,
            int daysValid = 0)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(incomingRefreshToken))
                return null;

            // ✅ Read refresh token expiration from configuration (default to 7 if not set)
            if (daysValid == 0)
            {
                daysValid = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays");
                if (daysValid == 0)
                {
                    daysValid = 7; // Fallback default
                }
            }

            // Find the matching token (active);
            var matches = await _repository.GetList(r =>
                r.UserId == userId &&
                r.Token == incomingRefreshToken &&
                r.CurrentState == 1);
            var match = matches?.FirstOrDefault();
            if (match == null)
            {
                // Token invalid or already revoked
                return null;
            }

            // ? Revoke the matched token with timestamp
            await _repository.UpdateFields(match.Id, token =>
            {
                token.CurrentState = 2;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "Token rotated";
            });
            // ? Issue a new refresh token
            var newTokenDto = new RefreshTokenDto
            {
                Token = GenerateSecureToken(64),
                UserId = userId,
                Expires = DateTime.UtcNow.AddDays(daysValid),
                CurrentState = 1,
                CreatedAt = DateTime.UtcNow
            };

            var dbToken = _mapper.Map<RefreshTokenDto, TbRefreshToken>(newTokenDto);
            await _repository.Add(dbToken);
            var resultDto = _mapper.Map<TbRefreshToken, RefreshTokenDto>(dbToken);
            return resultDto;
        }
        
    }

}