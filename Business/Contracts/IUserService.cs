using Business.DTOS;
using DataAccessLayer.UserModels;
using Domains;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Contracts
{
    /// <summary>
    /// Defines the contract for user authentication and account management services.
    /// This interface is implemented by BOTH UI and WebApi projects with different authentication strategies:
    /// 
    /// IMPLEMENTATIONS:
    /// 1. UI/Services/UserService.cs:
    ///    - Uses cookie-based authentication via SignInManager
    ///    - LoginAsync validates credentials AND signs in the user with cookies
    ///    - Used by Razor Pages controllers for browser-based UI authentication
    ///    
    /// 2. WebApi/Services/UserService.cs:
    ///    - Uses JWT token-based authentication
    ///    - LoginAsync ONLY validates credentials (does NOT sign in)
    ///    - AuthController generates JWT tokens after successful validation
    ///    - Used by API endpoints for stateless token-based authentication
    ///    
    /// KEY ARCHITECTURAL DISTINCTION:
    /// Both implementations handle user management (create, retrieve, password reset),
    /// but they serve completely different authentication flows:
    /// - UI → Cookie authentication for web browser navigation
    /// - WebApi → JWT tokens for stateless API requests
    /// 
    /// This separation allows the application to support hybrid authentication:
    /// users authenticate via UI (cookies), which then calls WebApi with JWT tokens.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Validates user credentials for authentication.
        /// IMPLEMENTATION DIFFERS BY PROJECT:
        /// - UI: Validates AND signs in with cookies
        /// - WebApi: ONLY validates (caller generates JWT tokens separately)
        /// </summary>
        /// <param name="loginDto">Login credentials (email, password, remember me)</param>
        /// <returns>Result indicating success/failure, lockout status, and errors</returns>
        Task<UserResultDto> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Signs out the current user.
        /// IMPLEMENTATION DIFFERS BY PROJECT:
        /// - UI: Clears authentication cookie via SignInManager
        /// - WebApi: Rarely used (JWT tokens are stateless; logout handled client-side)
        /// </summary>
        /// <returns>Completed task</returns>
        Task LogoutAsync();

        /// <summary>
        /// Registers a new user account with the specified role.
        /// IMPLEMENTATION DIFFERS BY PROJECT:
        /// - UI: May sign in user after registration (cookie-based)
        /// - WebApi: Does NOT sign in (user must login to get JWT tokens)
        /// </summary>
        /// <param name="registerDto">Registration data (email, password, name, phone, role)</param>
        /// <returns>Result indicating success/failure and validation errors</returns>
        Task<UserResultDto> RegisterAsync(UserDto registerDto);

        /// <summary>
        /// Retrieves a user by ID.
        /// Returns minimal user information (ID, email).
        /// </summary>
        /// <param name="id">User's ID (GUID as string)</param>
        /// <returns>UserDto or null if not found</returns>
        Task<UserDto> GetUserByIdAsync(string id);

        /// <summary>
        /// Retrieves a user by email address.
        /// Returns minimal user information (ID, email).
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>UserDto or null if not found</returns>
        Task<UserDto> GetUserByEmailAsync(string email);

        /// <summary>
        /// Retrieves all registered users.
        /// Returns minimal user information (ID, email).
        /// Typically used by admin endpoints.
        /// </summary>
        /// <returns>Collection of UserDto objects</returns>
        Task<IEnumerable<UserDto>> GetAllUsersAsync();

        /// <summary>
        /// Gets the currently authenticated user's ID (async version).
        /// IMPLEMENTATION DIFFERS BY PROJECT:
        /// - UI: Reads from cookie authentication claims
        /// - WebApi: Reads from JWT token claims
        /// </summary>
        /// <returns>User's GUID</returns>
        /// <exception cref="InvalidOperationException">Thrown if user is not authenticated</exception>
        Task<Guid> GetLoggedInUserAsync();

        /// <summary>
        /// Gets the currently authenticated user's ID (sync version).
        /// IMPLEMENTATION DIFFERS BY PROJECT:
        /// - UI: Reads from cookie authentication claims (throws if missing)
        /// - WebApi: Reads from JWT token claims (may return Guid.Empty for non-critical flows)
        /// </summary>
        /// <returns>User's GUID</returns>
        Guid GetLoggedInUser();

        /// <summary>
        /// Finds a user by email and returns the full Identity entity.
        /// Used in password reset flows and scenarios requiring direct user entity access.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>ApplicationUser entity or null if not found</returns>
        Task<ApplicationUser> FindByEmailAsync(string email);

        /// <summary>
        /// Generates a password reset token for the specified user.
        /// Token is URL-safe, time-limited, and single-use for security.
        /// </summary>
        /// <param name="user">User requesting password reset</param>
        /// <returns>Password reset token string</returns>
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);

        /// <summary>
        /// Resets a user's password using a valid reset token.
        /// SECURITY: Should return generic error messages to prevent username enumeration.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="token">Password reset token (from email)</param>
        /// <param name="newPassword">New password (must meet Identity requirements)</param>
        /// <returns>IdentityResult with success/failure and errors</returns>
        Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword);

        /// <summary>
        /// Changes a user's password for an authenticated user who knows their current password.
        /// This is different from ResetPasswordAsync which is for forgotten passwords (uses token).
        /// 
        /// SECURITY: Validates the current password before allowing the change.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="currentPassword">User's current password (for validation)</param>
        /// <param name="newPassword">New password (must meet Identity requirements)</param>
        /// <returns>IdentityResult with success/failure and errors</returns>
        Task<IdentityResult> ChangePasswordAsync(string email, string currentPassword, string newPassword);

        /// <summary>
        /// Updates an existing user's information (admin operation).
        /// Allows updating FirstName, LastName, Phone, and Email.
        /// Typically used by admin user management interfaces.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <param name="userDto">Updated user information</param>
        /// <returns>UserResultDto indicating success/failure and validation errors</returns>
        Task<UserResultDto> UpdateUserAsync(string userId, UserDto userDto);

        /// <summary>
        /// Locks a user account, preventing them from logging in.
        /// Sets LockoutEnd to a far future date (e.g., 100 years).
        /// Used by administrators to disable problematic accounts.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <returns>IdentityResult with success/failure</returns>
        Task<IdentityResult> LockUserAsync(string userId);

        /// <summary>
        /// Unlocks a user account, allowing them to log in again.
        /// Clears the LockoutEnd date.
        /// Used by administrators to re-enable locked accounts.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <returns>IdentityResult with success/failure</returns>
        Task<IdentityResult> UnlockUserAsync(string userId);

        /// <summary>
        /// Changes a user's role assignment.
        /// Removes the user from all existing roles and assigns the new role.
        /// Used by administrators to manage user permissions.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <param name="newRole">New role name to assign</param>
        /// <returns>IdentityResult with success/failure</returns>
        Task<IdentityResult> ChangeUserRoleAsync(string userId, string newRole);



















   

    }
}
