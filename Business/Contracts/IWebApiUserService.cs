using Business.DTOS;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Business.Contracts
{
    /// <summary>
    /// WebApi-specific user service contract.
    /// Extends IUserService with WebApi-only methods for email confirmation,
    /// password management, and forgot password flows.
    /// 
    /// KEY DIFFERENCE from IUIUserService:
    /// - WebApi handles email confirmation with welcome email send
    /// - WebApi sends forgot password emails directly
    /// - WebApi logs password changes with security audit trail
    /// - WebApi includes email sender parameter for sending confirmations
    /// 
    /// NOT used by UI - UI has its own IUIUserService contract
    /// with different account management methods.
    /// 
    /// IMPLEMENTATION: WebApi/Services/UserService.cs
    /// </summary>
    public interface IWebApiUserService : IUserService
    {
        /// <summary>
        /// Initiates password reset flow by generating reset token and sending email.
        /// 
        /// FLOW:
        /// 1. Lookup user by email
        /// 2. Generate password reset token (time-limited, single-use)
        /// 3. Build reset link with token
        /// 4. Send reset email (async, graceful failure)
        /// 5. Log password reset request
        /// 6. Return (always appears successful to prevent username enumeration)
        /// 
        /// SECURITY:
        /// - Always returns success (prevents username enumeration)
        /// - Email failures logged but don't break the flow
        /// - Logs attempts for non-existent users (security audit)
        /// 
        /// WebApi-specific: Handles entire forgot password workflow including email send
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="uiResetPasswordUrl">UI URL for password reset page (e.g., "https://localhost:7065/Account/ResetPassword")</param>
        /// <param name="emailSender">Email service to send reset link</param>
        /// <returns>Task (always completes successfully for security reasons)</returns>
        Task ForgotPasswordAsync(string email, string uiResetPasswordUrl);

        /// <summary>
        /// Confirms a user's email address using a confirmation token.
        /// 
        /// FLOW:
        /// 1. Validate token and userId parameters
        /// 2. Lookup user by ID
        /// 3. Confirm email using token
        /// 4. Send welcome email on success (best-effort)
        /// 5. Log confirmation success or detailed failure
        /// 
        /// SECURITY:
        /// - Token is single-use and time-limited
        /// - AllowAnonymous endpoint (user not yet authenticated)
        /// - Generic error messages prevent information disclosure
        /// - All attempts logged for audit trail
        /// 
        /// WebApi-specific: Sends welcome email on successful confirmation
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <param name="token">Email confirmation token from email link</param>
        /// <param name="emailSender">Email service to send welcome email</param>
        /// <returns>IdentityResult with success/failure status and validation errors</returns>
        Task<IdentityResult> ConfirmEmailAsync(string userId, string token, IEmailSender emailSender);

        /// <summary>
        /// Changes a user's password for an authenticated user who knows their current password.
        /// Different from ResetPasswordAsync which is for forgotten passwords (uses token).
        /// 
        /// SECURITY:
        /// - Requires current password verification (prevents unauthorized changes)
        /// - Validates against identity password policy
        /// - All password changes logged for audit trail
        /// - Logs success and detailed failure reasons
        /// 


        Task<(Claim[] Claims, UserDto User)> GetClaimsAsync(string email);
        Task<Claim[]> GetClaimsByIdAsync(string userId);


        Task<UserResultDto> RegisterAsync(UserDto userDto, Business.Contracts.IEmailSender emailSender, string uiBaseUrl = "https://localhost:7065");
    }
}
