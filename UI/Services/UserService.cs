using Business.Contracts;
using Business.Contracts.Logging;
using Business.DTOS;
using DataAccessLayer.UserModels;
using Domains;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using System.Text;
using System.Text.Json;



namespace UI.Services 
{
    /// <summary>
    /// UI-side user authentication service for cookie-based authentication in Razor Pages.
    /// This implementation uses ASP.NET Core Identity's SignInManager to handle cookie-based
    /// authentication flows for the web UI. It is SEPARATE from WebApi's UserService, which
    /// handles JWT token validation for API requests.
    /// 
    /// ARCHITECTURE NOTE:
    /// - UI Project: Uses THIS service → Cookie authentication via SignInManager
    /// - WebApi Project: Uses its own UserService → JWT token validation
    /// Both implement IUserService but serve completely different authentication mechanisms.
    /// </summary>
    public class UserService : IUIUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GenericApiClient _apiClient;
        private readonly ILogger<UserService> _logger;
        private readonly AccountAuditService _auditService;

       
        private readonly Business.Contracts.IEmailSender _emailSender;
        private readonly ISecurityEventLogger _securityEventLogger;
        /// <summary>
        /// Initializes the UI UserService with ASP.NET Core Identity managers.
        /// </summary>
        /// <param name="userManager">Manages user accounts in the Identity database</param>
        /// <param name="signInManager">Manages cookie-based sign-in/sign-out for UI authentication</param>
        /// <param name="httpContextAccessor">Provides access to the current HTTP context and user claims</param>
        /// <param name="apiClient">Generic API client for calling WebApi endpoints (if needed)</param>
        /// <param name="logger">Logger for debugging and monitoring</param>
        public UserService(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IHttpContextAccessor httpContextAccessor, 
            GenericApiClient apiClient, 
            ILogger<UserService> logger,
            AccountAuditService auditService,
            ISecurityEventLogger securityEventLogger,
            Business.Contracts.IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            _apiClient = apiClient;
            _logger = logger;
            _auditService = auditService;
            _securityEventLogger = securityEventLogger;
            _emailSender = emailSender;

        }

        /// <summary>
        /// Validates user credentials using ASP.NET Core Identity and establishes a sign-in session.
        /// This method is called by AccountController.Login() to validate credentials BEFORE
        /// calling the WebApi to obtain JWT tokens for API operations.
        /// 
        /// AUTHENTICATION FLOW:
        /// 1. AccountController calls this method to validate email/password
        /// 2. If successful, AccountController then calls WebApi /api/auth/login to get JWT tokens
        /// 3. AccountController builds claims with the JWT AccessToken and signs in via HttpContext.SignInAsync()
        /// 
        /// This method ONLY validates credentials and manages lockout - it does NOT create the final
        /// authentication cookie (that's done in AccountController after obtaining tokens).
        /// </summary>
        /// <param name="model">Login credentials (email, password, remember me flag)</param>
        /// <returns>UserResultDto indicating success/failure and any validation errors</returns>
        /// 

        /// <summary>
        /// Authenticates a user by email and password.
        /// A deactivated account is reactivated automatically, but only after the password has been
        /// verified, so nothing reveals the account's state to someone who doesn't know the password.
        /// Every outcome is written to the account audit trail.
        /// </summary>
        /// <param name="model">Login credentials (email, password, remember me flag)</param>
        /// <returns>UserResultDto indicating success/failure, lockout flags, and whether the account was reactivated</returns>
        // ============ 2) LoginAsync: replace the whole method ============

        public async Task<UserResultDto> LoginAsync(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                await _securityEventLogger.LogFailedLogin(model.Email, "Invalid credentials");
                return new UserResultDto { Success = false, Errors = new[] { "Email:Invalid credentials" } };
            }

            // The deactivated state is revealed only after the password is verified,
            // so the login form cannot be used to discover which emails have (deactivated) accounts.
            if (user.IsDeactivated)
            {
                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
                if (passwordCheck.Succeeded)
                {
                    await SendReactivationCodeAsync(user);
                    return new UserResultDto { Success = false, ReactivationCodeSent = true };
                }
                // Wrong password or locked out: fall through, the normal sign-in below handles and logs it.
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                await _auditService.LogAccountActionAsync(user.Id, "SuccessfulLogin", initiatedBy: model.Email, ipAddress: GetClientIpAddress());
                return new UserResultDto { Success = true };
            }

            if (result.IsLockedOut)
            {
                user = await _userManager.FindByEmailAsync(model.Email);

                // Admin locks are set to 100 years, temporary lockouts to minutes
                var isAdminLock = user!.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddHours(1);

                await _auditService.LogAccountActionAsync(
                    userId: user.Id,
                    action: "FailedLoginAttempt_LockedOut",
                    initiatedBy: model.Email,
                    reason: isAdminLock ? "Admin lockout" : "Temporary lockout after failed attempts",
                    ipAddress: GetClientIpAddress());

                await _securityEventLogger.LogFailedLogin(model.Email, isAdminLock ? "Admin lockout" : "Account temporarily locked");

                // Identity reports "locked out" before it checks the password. The specific message is shown
                // only when the password is correct, so the form cannot be used to find locked accounts.
                string error;
                if (await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    var minutes = Math.Max(1, (int)Math.Ceiling((user.LockoutEnd!.Value - DateTimeOffset.UtcNow).TotalMinutes));
                    error = isAdminLock
                        ? "Email:This account has been locked by an administrator. Please contact support."
                        : $"Email:Too many failed attempts. Please try again in {minutes} minute(s).";
                }
                else
                {
                    error = "Password:Invalid credentials";
                }

                return new UserResultDto
                {
                    Success = false,
                    IsLockedOut = true,
                    IsAdminLocked = isAdminLock,
                    LockoutEnd = user.LockoutEnd,
                    Errors = new[] { error }
                };
            }

            await _auditService.LogAccountActionAsync(user.Id, "FailedLoginAttempt_InvalidPassword", initiatedBy: model.Email, ipAddress: GetClientIpAddress());
            await _securityEventLogger.LogFailedLogin(model.Email, "Invalid credentials");

            return new UserResultDto { Success = false, Errors = new[] { "Password:Invalid credentials" } };
        }
       
        private const string ReactivationTokenPurpose = "ReactivateAccount";

        /// <summary>
        /// Emails a 6-digit code. Identity's email token provider derives it from the user's security stamp,
        /// so nothing is stored in the database, it expires on its own (a few minutes) and
        /// it stops working as soon as the stamp changes (we change it after a successful use).
        /// </summary>
        public async Task SendReactivationCodeAsync(ApplicationUser user)
        {
            var code = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, ReactivationTokenPurpose);

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Reactivate your MyShipping account",
                $"Your reactivation code is {code}. It expires in a few minutes. If you did not try to sign in, change your password.");



            _securityEventLogger.LogReactivationCodeSent(user.Id.ToString(), user.Email!);

            // The code itself is never written to the audit table
            await _auditService.LogAccountActionAsync(
                user.Id, "ReactivationCodeSent", initiatedBy: user.Email!,
                reason: "Correct password on a deactivated account", ipAddress: GetClientIpAddress());
        }

        public async Task<UserResultDto> ReactivateWithCodeAsync(string email, string code)
        {
            // One generic error for every failure, so the response reveals nothing about the account
            var invalid = new UserResultDto { Success = false, Errors = new[] { "The code is invalid or has expired." } };

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsDeactivated || await _userManager.IsLockedOutAsync(user))
                return invalid;

            var isValid = await _userManager.VerifyUserTokenAsync(
                user, TokenOptions.DefaultEmailProvider, ReactivationTokenPurpose, code?.Trim() ?? string.Empty);

            if (!isValid)
            {
                // Counts toward Identity lockout, which limits guessing of the 6-digit code
                await _userManager.AccessFailedAsync(user);
                await _auditService.LogAccountActionAsync(
                    user.Id, "FailedReactivationCode", initiatedBy: email,
                    reason: "Wrong or expired code", ipAddress: GetClientIpAddress());
                return invalid;
            }

            _securityEventLogger.LogReactivationCodeFailed(email, "Wrong or expired code");

            user.IsDeactivated = false;
            user.DeactivatedAt = null;
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
                return new UserResultDto { Success = false, Errors = update.Errors.Select(e => e.Description) };

            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);   // makes the code single-use

            await _auditService.LogAccountActionAsync(
                user.Id, "Reactivated", initiatedBy: email,
                reason: "Self-service reactivation, email code verified", ipAddress: GetClientIpAddress());

            _securityEventLogger.LogAccountReactivated(user.Id.ToString(), email, email);
            return new UserResultDto { Success = true };
        }

 

        /// <summary>
        /// Signs out the current user by clearing the authentication cookie.
        /// Called by AccountController.LogoutConfirmed() after the user confirms logout.
        /// </summary>
        /// <returns>Completed task</returns>
        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
           
        }

        /// <summary>
        /// Registers a new user account in the ASP.NET Core Identity database.
        /// Creates the user with email/password and assigns the specified role.
        /// 
        /// NOTE: This method attempts to sign in the user immediately after registration,
        /// but the sign-in may fail if email confirmation is required. The AccountController
        /// typically redirects to Login after successful registration anyway.
        /// </summary>
        /// <param name="registerDto">User registration data (email, password, name, role)</param>
        /// <returns>UserResultDto indicating success/failure and any validation errors</returns>
        /// 
        public async Task<UserResultDto> RegisterAsync(UserDto registerDto)
        {
            var errors = new List<string>();

            // Check if email exists
            var existingEmail = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingEmail != null)
                errors.Add("Email:Email is already in use.");

            // Check if username exists (assuming username is email)
            var existingUserName = await _userManager.FindByNameAsync(registerDto.Email);
            if (existingUserName != null)
                errors.Add("Email:Username is already in use.");

            // Check if phone exists
            var existingPhone = _userManager.Users.FirstOrDefault(u => u.Phone == registerDto.Phone);
            if (existingPhone != null)
                errors.Add("Phone:Phone number is already in use.");

            // Password validation (Identity enforces the full policy; this gives an early, matching message)
            if (registerDto.Password.Length < 8)
                errors.Add("Password:Password must be at least 8 characters.");

            if (registerDto.Password != registerDto.ConfirmPassword)
                errors.Add("ConfirmPassword:Password and confirmation password do not match");

            if (errors.Any())
            {
                // Log failed registration with validation reason
                var failureReason = errors.Count == 1
                    ? errors.First()
                    : $"Multiple validation errors: {string.Join("; ", errors)}";

                _securityEventLogger.LogRegistrationFailed(registerDto.Email, failureReason);
                return new UserResultDto
                {
                    Success = false,
                    Errors = errors
                };
            }

            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Phone = registerDto.Phone,
                NotifyByEmail = registerDto.NotifyByEmail,
                NotifyBySms = registerDto.NotifyBySms,
                NotifyShipmentStatusUpdates = registerDto.NotifyShipmentStatusUpdates,
                NotifyMarketing = registerDto.NotifyMarketing
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                _securityEventLogger.LogRegistrationFailed(
                    registerDto.Email,
                    $"User creation failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");

                // Map identity errors to the correct field where possible
                var identityErrors = result.Errors.Select(e =>
                    !string.IsNullOrEmpty(e.Code) && e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)
                        ? $"Password:{e.Description}"
                        : $"Email:{e.Description}");

                return new UserResultDto { Success = false, Errors = identityErrors };
            }

            // Public registration always creates a standard user.
            // Never use registerDto.Role here: privileged roles are assigned from the admin dashboard.
            var roleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!roleResult.Succeeded)
            {
                _securityEventLogger.LogRegistrationFailed(
                    registerDto.Email,
                    $"Role assignment failed: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");

                await _userManager.DeleteAsync(user); // don't leave a user without a role
                return new UserResultDto
                {
                    Success = false,
                    Errors = roleResult.Errors.Select(e => $"Role:{e.Description}")
                };
            }

            _securityEventLogger.LogRegistrationSuccess(user.Id.ToString(), registerDto.Email);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return new UserResultDto { Success = true };
        }

  
        /// <summary>
        /// Retrieves a user account by ID and returns it as a UserDto with role information.
        /// Used by AccountController and other UI controllers to fetch user details.
        /// </summary>
        /// <param name="userid">User's ID as a string (GUID format)</param>
        /// <returns>UserDto with user details and primary role, or null if user not found</returns>
        public async Task<UserDto> GetUserByIdAsync(string userid)
        {
            var user = await _userManager.FindByIdAsync(userid);
            return await MapUserToUserDtoAsync(user);

        }

        /// <summary>
        /// Retrieves a user account by email address and returns it as a UserDto with role information.
        /// Used extensively in login flow to fetch user details after credential validation.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>UserDto with user details and primary role, or null if user not found</returns>
        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return await MapUserToUserDtoAsync(user);

        }

        /// <summary>
        /// Retrieves all registered users from the database with complete details.
        /// Returns full user information including roles and lockout status.
        /// Used for admin user management features.
        /// </summary>
        /// <returns>Collection of UserDto objects containing complete user details</returns>
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var userDto = await MapUserToUserDtoAsync(user);
                if (userDto != null)
                {
                    userDto.IsLockedOut = await _userManager.IsLockedOutAsync(user);
                    userDtos.Add(userDto);
                }
            }

            return userDtos;
        }

        /// <summary>
        /// Gets the currently logged-in user's ID from the HTTP context claims.
        /// Synchronous version - use GetLoggedInUserAsync() when possible.
        /// 
        /// IMPORTANT: Throws InvalidOperationException if user is not authenticated or ID claim is missing.
        /// This prevents silent failures that could lead to security issues.
        /// </summary>
        /// <returns>User's GUID from NameIdentifier claim</returns>
        /// <exception cref="InvalidOperationException">Thrown if user is not authenticated or ID claim is missing</exception>
        public Guid GetLoggedInUser()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if userIdString is null or empty before attempting to parse
            if (string.IsNullOrEmpty(userIdString))
            {
                // Option 1: Throw an exception (Recommended for critical security flows)
                throw new InvalidOperationException("User ID not found in claims. User might not be authenticated.");

                // Option 2: Return Guid.Empty (Less recommended, can lead to silent failures)
                // return Guid.Empty;

                // Option 3: Return a nullable Guid (If your method signature allows it)
                //return null;
            }

            return Guid.Parse(userIdString);
        }

        /// <summary>
        /// Gets the currently logged-in user's ID from the HTTP context claims (async version).
        /// Preferred over GetLoggedInUser() for consistency with async/await patterns.
        /// 
        /// IMPORTANT: Throws InvalidOperationException if:
        /// - HTTP context is null
        /// - User is not authenticated
        /// - NameIdentifier claim is missing
        /// </summary>
        /// <returns>User's GUID from NameIdentifier claim</returns>
        /// <exception cref="InvalidOperationException">Thrown if user is not authenticated or claims are missing</exception>
        public async Task<Guid> GetLoggedInUserAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("No HTTP context found.");
            }

            var claimsPrincipal =  httpContext.User as ClaimsPrincipal;
            if (claimsPrincipal == null)
            {
                throw new InvalidOperationException("No user is logged in.");
            }

            var userIdClaim = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new InvalidOperationException("No user ID claim found.");
            }

            return Guid.Parse(userIdClaim);
        }

        /// <summary>
        /// Finds a user by email address and returns the full ApplicationUser entity.
        /// Used in password reset flows and other scenarios requiring direct access to Identity user.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>ApplicationUser entity, or null if not found</returns>
        public async Task<ApplicationUser> FindByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        /// <summary>
        /// Generates a password reset token for the specified user.
        /// This token is sent via email and used to verify password reset requests.
        /// Tokens are time-limited and single-use for security.
        /// </summary>
        /// <param name="user">The user requesting password reset</param>
        /// <returns>URL-safe password reset token string</returns>
        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        /// <summary>
        /// Resets a user's password using a valid reset token.
        /// The token must have been generated via GeneratePasswordResetTokenAsync and not expired.
        /// 
        /// SECURITY: Returns generic "Unable to reset password" message on failure to avoid
        /// revealing whether the email exists in the system.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="token">Password reset token from email</param>
        /// <param name="newPassword">New password (must meet Identity password requirements)</param>
        /// <returns>IdentityResult indicating success or validation errors</returns>
        public async Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Return a failed result to avoid revealing user existence.
                return IdentityResult.Failed(new IdentityError { Description = "Unable to reset password." });
            }

            return await _userManager.ResetPasswordAsync(user, token, newPassword);

            //var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            //return result;
        }

        /// <summary>
        /// Changes a user's password for an authenticated user who knows their current password.
        /// This is different from ResetPasswordAsync which is for forgotten passwords (uses token).
        /// 
        /// SECURITY: Validates the current password before allowing the change.
        /// Logs all security-relevant events (success and failures).
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="currentPassword">User's current password (for validation)</param>
        /// <param name="newPassword">New password (must meet Identity password requirements)</param>
        /// <returns>IdentityResult indicating success or validation errors</returns>
        public async Task<IdentityResult> ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            // ✅ Validate input
            if (string.IsNullOrWhiteSpace(email))
            {
                _securityEventLogger.LogPasswordResetFailed("Unknown", "Email cannot be empty");
                return IdentityResult.Failed(new IdentityError { Description = "Unable to change password." });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // ✅ Log failed attempt (security event - someone trying to change password for non-existent user)
                _securityEventLogger.LogPasswordResetFailed(email, "User not found");

                // Return a failed result to avoid revealing user existence
                return IdentityResult.Failed(new IdentityError { Description = "Unable to change password." });
            }

            // ✅ Attempt password change
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            // ✅ Log the result
            if (result.Succeeded)
            {
                // Log successful password change
                _securityEventLogger.LogPasswordResetSuccess(user.Id.ToString(), email);

                // Also log to audit trail
                await _auditService.LogAccountActionAsync(
                    userId: user.Id,
                    action: "PasswordChanged",
                    initiatedBy: email,
                    reason: "User changed their password",
                    ipAddress: GetClientIpAddress()
                );
            }
            else
            {
                // ✅ Log password change failure with reason
                var failureReason = string.Join("; ", result.Errors.Select(e => e.Description));
                _securityEventLogger.LogPasswordResetFailed(email, $"Password change failed: {failureReason}");

                // Also log to audit trail
                await _auditService.LogAccountActionAsync(
                    userId: user.Id,
                    action: "PasswordChangeFailed",
                    initiatedBy: email,
                    reason: failureReason,
                    ipAddress: GetClientIpAddress()
                );
            }

            return result;
        }

        /// <summary>
        /// Updates an existing user's information (admin operation).
        /// Allows administrators to update user details including name, phone, and email.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <param name="userDto">Updated user information</param>
        /// <returns>UserResultDto indicating success/failure and validation errors</returns>
        public async Task<UserResultDto> UpdateUserAsync(string userId, UserDto userDto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new UserResultDto
                {
                    Success = false,
                    Errors = new[] { "User not found." }
                };
            }

            var errors = new List<string>();

            // Check if new email is already in use by another user
            if (user.Email != userDto.Email)
            {
                var existingEmail = await _userManager.FindByEmailAsync(userDto.Email);
                if (existingEmail != null && existingEmail.Id != userId)
                    errors.Add("Email:Email is already in use by another user.");
            }

            // Check if new phone is already in use by another user
            if (user.Phone != userDto.Phone)
            {
                var existingPhone = _userManager.Users.FirstOrDefault(u => u.Phone == userDto.Phone && u.Id != userId);
                if (existingPhone != null)
                    errors.Add("Phone:Phone number is already in use by another user.");
            }

            if (errors.Any())
            {
                return new UserResultDto
                {
                    Success = false,
                    Errors = errors
                };
            }

            // Update user properties
            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;
            user.Phone = userDto.Phone;
            user.Email = userDto.Email;
            user.UserName = userDto.Email; // Keep username in sync with email
            user.NotifyByEmail = userDto.NotifyByEmail;
            user.NotifyBySms = userDto.NotifyBySms;
            user.NotifyShipmentStatusUpdates = userDto.NotifyShipmentStatusUpdates;
            user.NotifyMarketing = userDto.NotifyMarketing;
            user.DefaultCountryId = userDto.DefaultCountryId;
            user.DefaultCityId = userDto.DefaultCityId;
            user.DefaultCarrierId = userDto.DefaultCarrierId;
            user.DefaultShippingPackageId = userDto.DefaultShippingPackageId;
            user.DefaultShippingTypeId = userDto.DefaultShippingTypeId;
            user.IsDeactivated = userDto.IsDeactivated;
            user.DeactivatedAt = userDto.DeactivatedAt;
           
            

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return new UserResultDto
                {
                    Success = false,
                    Errors = result.Errors?.Select(e => e.Description)
                };
            }

            return new UserResultDto { Success = true };
        }

   

        public async Task<IdentityResult> LockUserAsync(string userId, string adminEmail, string? reason = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            // Lockout must be enabled first, otherwise SetLockoutEndDateAsync refuses to set the end date
            var enableResult = await _userManager.SetLockoutEnabledAsync(user, true);
            if (!enableResult.Succeeded)
            {
                return enableResult;
            }

            // 100 years from now is effectively permanent; LoginAsync treats anything over 1 hour as an admin lock
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            if (!result.Succeeded)
            {
                return result;
            }

            await _auditService.LogAccountActionAsync(
                userId: userId,
                action: "LockedByAdmin",
                initiatedBy: adminEmail,
                reason: reason ?? "Admin locked the account",
                ipAddress: GetClientIpAddress());

            return result;
        }

        public async Task<IdentityResult> UnlockUserAsync(string userId, string adminEmail, string? reason = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            // Clear the lockout and the failed-attempt counter, otherwise the next wrong password
            // can lock the user again immediately
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
            {
                return result;
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogAccountActionAsync(
                userId: userId,
                action: "UnlockedByAdmin",
                initiatedBy: adminEmail,
                reason: reason ?? "Admin unlocked the account",
                ipAddress: GetClientIpAddress());

            return result;
        }

     

        /// <summary>
        /// Changes a user's role assignment.
        /// Removes the user from all existing roles and assigns the new role.
        /// Used by administrators to manage user permissions and access levels.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <param name="newRole">New role name to assign (e.g., "Admin", "User", "Reviewer")</param>
        /// <returns>IdentityResult with success/failure</returns>
        public async Task<IdentityResult> ChangeUserRoleAsync(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            // Get current roles and remove them
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return removeResult;
                }
            }

            // Add the new role
            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            return addResult;
        }
        /// <summary>
        /// Signs out all other active sessions for the current user by updating their security stamp.
        /// This invalidates all existing authentication cookies for the user, forcing re-login on other devices.
        /// Called by AccountController.SignOutOtherDevices() to manage multi-device sessions.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>AccountOperationResultDto with success/failure</returns>
        public async Task<AccountOperationResultDto> SignOutOtherDevicesAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new AccountOperationResultDto
                {
                    Success = false,
                    RequiresLogin = true
                };
            }

            var appUser = await _userManager.FindByEmailAsync(email);
            if (appUser == null)
            {
                return new AccountOperationResultDto
                {
                    Success = false,
                    RequiresLogin = true
                };
            }

            try
            {
                // Update security stamp to invalidate all existing tokens/cookies
                var updateResult = await _userManager.UpdateSecurityStampAsync(appUser);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Failed to update security stamp for user {Email}. Errors: {Errors}",
                        email, string.Join(", ", updateResult.Errors.Select(e => e.Description)));

                    return new AccountOperationResultDto
                    {
                        Success = false,
                        Errors = updateResult.Errors.Select(e => e.Description)
                    };
                }

                // Refresh the current session with the new security stamp
                await _signInManager.RefreshSignInAsync(appUser);

                _logger.LogWarning("✅ Security stamp updated for user {Email}. Other sessions signed out.", email);

                return new AccountOperationResultDto
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating security stamp for user {Email}", email);
                return new AccountOperationResultDto
                {
                    Success = false,
                    Errors = new[] { ex.Message }
                };
            }
        }

        /// <summary>
        /// Gets the current ApplicationUser entity from the HTTP context claims.
        /// Used internally when ApplicationUser object is needed instead of UserDto.
        /// </summary>
        /// <param name="email">User's email address (typically from identity name)</param>
        /// <returns>ApplicationUser entity, or null if not found</returns>
        public async Task<ApplicationUser> GetCurrentApplicationUserAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await _userManager.FindByEmailAsync(email);
        }
        public async Task<UserDto?> GetCurrentUserByIdentityNameAsync(string? identityName)
        {
            if (string.IsNullOrWhiteSpace(identityName))
            {
                return null;
            }

            return await GetUserByEmailAsync(identityName);
        }
        public async Task<PersonalDataExportDto> BuildPersonalDataExportAsync(string? identityName)
        {
            var user = await GetCurrentUserByIdentityNameAsync(identityName);
            if (user == null)
            {
                return new PersonalDataExportDto
                {
                    Success = false,
                    RequiresLogin = string.IsNullOrWhiteSpace(identityName)
                };
            }

            // ✅ Get audit logs for this user
            var auditLogs = await _auditService.GetUserAuditLogsAsync(user.Id.ToString(), limit: 100);

            // ✅ EXPANDED: Include all missing properties
            var payload = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Phone,

                // ✅ NEW: Notification Settings
                NotificationPreferences = new
                {
                    user.NotifyByEmail,
                    user.NotifyBySms,
                    user.NotifyShipmentStatusUpdates,
                    user.NotifyMarketing
                },

                // ✅ NEW: Shipping Preferences
                ShippingDefaults = new
                {
                    user.DefaultCountryId,
                    user.DefaultCityId,
                    user.DefaultCarrierId,
                    user.DefaultShippingPackageId,
                    user.DefaultShippingTypeId
                },

                // ✅ NEW: Account Status
                AccountStatus = new
                {
                    IsDeactivated = user.IsDeactivated,
                    DeactivatedAt = user.DeactivatedAt
                },

                // ✅ NEW: Audit Trail
                AuditLogs = auditLogs.Select(a => new
                {
                    a.Action,
                    a.Reason,
                    a.InitiatedBy,
                    a.IpAddress,
                    a.Timestamp
                }).ToList(),

                ExportedAtUtc = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var bytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"myshipping-personal-data-{DateTime.UtcNow:yyyyMMddHHmmss}.json";

            return new PersonalDataExportDto
            {
                Success = true,
                Content = bytes,
                FileName = fileName
            };
        }
        

        /// <summary>
        /// Maps an ApplicationUser entity to a UserDto with all properties.
        /// Used internally by all methods that need to return user data to controllers.
        /// </summary>
        /// <param name="user">ApplicationUser entity to map</param>
        /// <returns>UserDto with all properties populated</returns>
        private async Task<UserDto> MapUserToUserDtoAsync(ApplicationUser user)
        {
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = Guid.Parse(user.Id),
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "User",
                NotifyByEmail = user.NotifyByEmail,
                NotifyBySms = user.NotifyBySms,
                NotifyShipmentStatusUpdates = user.NotifyShipmentStatusUpdates,
                NotifyMarketing = user.NotifyMarketing,
                DefaultCountryId = user.DefaultCountryId,
                DefaultCityId = user.DefaultCityId,
                DefaultCarrierId = user.DefaultCarrierId,
                DefaultShippingPackageId = user.DefaultShippingPackageId,
                DefaultShippingTypeId = user.DefaultShippingTypeId,
                IsDeactivated = user.IsDeactivated,
                DeactivatedAt = user.DeactivatedAt
            };
        }

        ///// <summary>
        ///// Helper to find and validate a user by email, handling null checks consistently.
        ///// </summary>
        //private async Task<ApplicationUser> FindUserByEmailOrThrowAsync(string email)
        //{
        //    if (string.IsNullOrWhiteSpace(email))
        //        throw new ArgumentException("Email cannot be null or empty.", nameof(email));

        //    return await _userManager.FindByEmailAsync(email);
        //}


        /// <summary>
        /// Deactivates the current user's account permanently.
        /// Unlike LockUserAsync, this is a user-initiated action that marks the account as deactivated.
        /// Admins CANNOT reactivate deactivated accounts (use for GDPR/privacy compliance).
        /// 
        /// DIFFERENCE FROM LOCKOUT:
        /// - Lockout: Temporary security measure, reversible by admin
        /// - Deactivation: Permanent user choice, not reversible by admin
        /// </summary>
        /// <param name="identityName">User's email address (from identity name)</param>
        /// <returns>AccountOperationResultDto with success/failure</returns>
        /// 
             // ✅ UPDATED: DeactivateCurrentUserAccountAsync with audit trail
        public async Task<AccountOperationResultDto> DeactivateCurrentUserAccountAsync(string? identityName)
        {
            if (string.IsNullOrWhiteSpace(identityName))
            {
                return new AccountOperationResultDto
                {
                    Success = false,
                    RequiresLogin = true
                };
            }

            var appUser = await _userManager.FindByEmailAsync(identityName);
            if (appUser == null)
            {
                return new AccountOperationResultDto
                {
                    Success = false,
                    RequiresLogin = true
                };
            }

            try
            {
                // Check if already deactivated
                if (appUser.IsDeactivated)
                {
                    _logger.LogWarning("⚠️ User {Email} attempted to deactivate an already-deactivated account", identityName);
                    return new AccountOperationResultDto
                    {
                        Success = false,
                        Errors = new[] { "This account is already deactivated." }
                    };
                }

                // Mark user as deactivated (permanent, user-initiated)
                appUser.IsDeactivated = true;
                appUser.DeactivatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(appUser);
                if (!result.Succeeded)
                {
                    _logger.LogError("❌ Failed to deactivate user {Email}. Errors: {Errors}",
                        identityName, string.Join(", ", result.Errors.Select(e => e.Description)));

                    return new AccountOperationResultDto
                    {
                        Success = false,
                        Errors = result.Errors.Select(e => e.Description)
                    };
                }

                // ✅ NEW: Log deactivation to audit trail
                var ipAddress = GetClientIpAddress();
                await _auditService.LogAccountActionAsync(
                    userId: appUser.Id,
                    action: "Deactivated",
                    initiatedBy: identityName,
                    reason: "User-initiated account deactivation",
                    ipAddress: ipAddress
                );

                // Sign out the current session after deactivation
                await LogoutAsync();

                _logger.LogWarning("✅ User {Email} has deactivated their account at {Time}",
                    identityName, appUser.DeactivatedAt);

                return new AccountOperationResultDto
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deactivating user {Email}", identityName);
                return new AccountOperationResultDto
                {
                    Success = false,
                    Errors = new[] { ex.Message }
                };
            }
        }

        // ✅ NEW: ReactivateUserAsync for admin operations with audit trail
        public async Task<IdentityResult> ReactivateUserAsync(string userId, string adminEmail, string? reason = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            if (!user.IsDeactivated)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User account is not deactivated." });
            }

            try
            {
                // Clear deactivation flags
                user.IsDeactivated = false;
                user.DeactivatedAt = null;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogError("❌ Failed to reactivate user {UserId}. Errors: {Errors}",
                        userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return result;
                }

                // ✅ NEW: Log reactivation to audit trail
                var ipAddress = GetClientIpAddress();
                await _auditService.LogAccountActionAsync(
                    userId: user.Id,
                    action: "Reactivated",
                    initiatedBy: adminEmail,
                    reason: reason ?? "Admin-initiated account reactivation",
                    ipAddress: ipAddress
                );

                _logger.LogDebug("✅ Admin {Admin} reactivated user {UserId} at {Time}",
                    adminEmail, userId, DateTime.UtcNow);

                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error reactivating user {UserId}", userId);
                return IdentityResult.Failed(new IdentityError { Description = ex.Message });
            }
        }

        // ✅ NEW: Helper method to get client IP address
        private string? GetClientIpAddress()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Connection?.RemoteIpAddress != null)
                {
                    return httpContext.Connection.RemoteIpAddress.ToString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Reactivates a deactivated user account (admin operation only).
        /// This should require admin approval and possibly email verification.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <returns>IdentityResult with success/failure</returns>
        public async Task<IdentityResult> ReactivateUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            if (!user.IsDeactivated)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User account is not deactivated." });
            }

            try
            {
                // Clear deactivation flags
                user.IsDeactivated = false;
                user.DeactivatedAt = null;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogWarning("✅ Admin reactivated user {UserId}", userId);
                }
                else
                {
                    _logger.LogError("❌ Failed to reactivate user {UserId}. Errors: {Errors}",
                        userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error reactivating user {UserId}", userId);
                return IdentityResult.Failed(new IdentityError { Description = ex.Message });
            }
        }

        public async Task<string?> GetUserDisplayNameAsync(Guid? userId)
        {
            if (!userId.HasValue) return null;

            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null) return null;

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return !string.IsNullOrWhiteSpace(fullName) ? fullName : user.UserName;
        }

        public async Task<Dictionary<Guid, string>> GetUserDisplayNamesAsync(IEnumerable<Guid?> userIds)
        {
            var validIds = userIds.Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList();
            if (!validIds.Any()) return new Dictionary<Guid, string>();

            var strIds = validIds.Select(id => id.ToString()).ToList();

            var users = await _userManager.Users
                .Where(u => strIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    DisplayName = !string.IsNullOrWhiteSpace(u.FirstName + " " + u.LastName)
                        ? (u.FirstName + " " + u.LastName).Trim()
                        : u.UserName
                })
                .ToDictionaryAsync(u => Guid.Parse(u.Id), u => u.DisplayName);

            return users;
        }

    }
}


