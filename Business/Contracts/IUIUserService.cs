using Business.DTOS;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts
{
    public interface IUIUserService : IUserService
    {



        
        /// <summary>
        /// Registers a new user account with the specified role.
        /// IMPLEMENTATION DIFFERS BY PROJECT:
        /// - UI: May sign in user after registration (cookie-based)
        /// - WebApi: Does NOT sign in (user must login to get JWT tokens)
        /// </summary>
        /// <param name="registerDto">Registration data (email, password, name, phone, role)</param>
        /// <returns>Result indicating success/failure and validation errors</returns>
        /// 
        Task<UserResultDto> RegisterAsync(UserDto registerDto);

        /// <summary>
        /// Retrieves a user by email from the supplied identity name value.
        /// Returns null when identity name is empty.
        /// </summary>
        /// <param name="identityName">Current identity name (usually email)</param>
        /// <returns>UserDto or null</returns>
        Task<UserDto?> GetCurrentUserByIdentityNameAsync(string? identityName);

        /// <summary>
        /// Builds personal data export payload for the current user identity name.
        /// </summary>
        /// <param name="identityName">Current identity name (usually email)</param>
        /// <returns>Export DTO with content and file metadata</returns>
        Task<PersonalDataExportDto> BuildPersonalDataExportAsync(string? identityName);

        /// <summary>
        /// Deactivates account (long lockout) for current user identity name.
        /// </summary>
        /// <param name="identityName">Current identity name (usually email)</param>
        /// <returns>Operation result</returns>
        Task<AccountOperationResultDto> DeactivateCurrentUserAccountAsync(string? identityName);



        Task<AccountOperationResultDto> SignOutOtherDevicesAsync(string email);


        Task<IdentityResult> ReactivateUserAsync(string userId, string adminEmail, string? reason = null);


        
    }
}
