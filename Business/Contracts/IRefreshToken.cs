using Business.DTOS;
using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts
{
    public interface IRefreshToken : IBaseService<TbRefreshToken, RefreshTokenDto>
    {
        

        //public RefreshTokenDto GetByToken(string token);
        
        public Task<bool> RefreshToken(RefreshTokenDto tokenDto);

        // Issue a new refresh token for the user (revokes existing active ones)
        Task<RefreshTokenDto> IssueRefreshTokenAsync(string userId, string createdBy = null, int daysValid = 7);

        // Validate incoming refresh token and rotate (revoke old, issue new)
        Task<RefreshTokenDto> RotateRefreshTokenAsync(string userId, string incomingRefreshToken, string createdBy = null, int daysValid = 7);

        // Helper: find user id by refresh token (used by controller)
        Task<string> GetUserIdByTokenAsync(string refreshToken);



        // ✅ ADD THESE:
        Task<List<RefreshTokenDto>> GetAllUserTokensAsync(string userId);
        Task UpdateTokenAsync(RefreshTokenDto tokenDto);
        Task RevokeAllUserTokensAsync(string userId, string reason = "Security alert");
        Task<RefreshTokenDto> RotateRefreshTokenWithTrackingAsync(string userId, string incomingRefreshToken, int daysValid = 7);

       
  

    }
}

