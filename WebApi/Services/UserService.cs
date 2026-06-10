using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Models;
using DataAccessLayer.UserModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace WebApi.Services 
{
    /// <summary>
    /// WebApi-side user authentication service for JWT token-based authentication.
    /// This implementation validates user credentials and is used by AuthController
    /// to authenticate API requests. It is SEPARATE from UI's UserService, which
    /// handles cookie-based authentication for Razor Pages.
    /// 
    /// ARCHITECTURE NOTE:
    /// - WebApi Project: Uses THIS service → Validates credentials for JWT token generation
    /// - UI Project: Uses its own UserService → Cookie authentication via SignInManager
    /// Both implement IUserService but serve completely different authentication flows:
    ///   • UI UserService: Signs in users with cookies for browser-based navigation
    ///   • WebApi UserService: Validates credentials so AuthController can generate JWT tokens
    /// 
    /// AUTHENTICATION FLOW:
    /// 1. UI calls WebApi /api/auth/login with email/password
    /// 2. AuthController calls THIS service's LoginAsync() to validate credentials
    /// 3. If valid, AuthController generates JWT access token + refresh token
    /// 4. UI stores access token in claims and refresh token in HttpOnly cookie
    /// 5. Subsequent API calls from UI include the JWT access token in Authorization header
    /// </summary>
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IRefreshTokenRetriver _refreshTokenRetriver;
        private readonly TokenService _tokenService;
        private readonly ILogger<UserService> _logger;
       

        /// <summary>
        /// Initializes the WebApi UserService with dependencies for credential validation and token management.
        /// </summary>
        /// <param name="userManager">ASP.NET Core Identity UserManager for user account operations</param>
        /// <param name="signInManager">SignInManager (used for some Identity operations, but NOT for cookie sign-in in WebApi)</param>
        /// <param name="_refreshTokenRetriver">Service to retrieve stored refresh tokens from database</param>
        /// <param name="httpContextAccessor">Provides access to current HTTP context and JWT claims</param>
        /// <param name="tokenService">Generates and validates JWT access tokens and refresh tokens</param>
        /// <param name="logger">Logger for authentication events and errors</param>
        public UserService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, 
            IRefreshTokenRetriver _refreshTokenRetriver,
            IHttpContextAccessor httpContextAccessor,
            TokenService tokenService, ILogger<UserService> logger
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            this._refreshTokenRetriver = _refreshTokenRetriver;
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// Validates user credentials for JWT token generation.
        /// Called by AuthController.Login() to verify email/password before issuing tokens.
        /// 
        /// IMPORTANT DIFFERENCES from UI UserService.LoginAsync():
        /// - This method ONLY validates credentials (does NOT sign in with cookies)
        /// - Handles lockout detection and failed attempt tracking
        /// - Returns success/failure so AuthController can generate JWT tokens
        /// - UI's version signs in via SignInManager; this one does NOT
        /// 
        /// SECURITY FEATURES:
        /// - Checks lockout status BEFORE password validation
        /// - Records failed login attempts via AccessFailedAsync
        /// - Auto-locks account after 5 failed attempts (configured in Identity options)
        /// - Returns generic "Invalid email or password" to prevent username enumeration
        /// </summary>
        /// <param name="loginDto">Login credentials (email, password, optional remember me flag)</param>
        /// <returns>UserResultDto with success/failure, lockout info, and error messages</returns>
        public async Task<UserResultDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new UserResultDto
                {
                    Success = false,
                    Errors = new[] { "Invalid email or password" }
                };
            }

            // ✅ STEP 1: Check lockout BEFORE password validation
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                return new UserResultDto
                {
                    Success = false,
                    IsLockedOut = true, // ✅ Add this property to LoginResult
                    LockoutEnd = lockoutEnd,
                    Errors = new[]
                    {
                        //$"Account locked until {lockoutEnd?.LocalDateTime:yyyy-MM-dd HH:mm:ss}. Please try again later."
                        $"Account locked due to multiple failed attempts. Contact your admin "
                    }
                };
            }

            // ✅ STEP 2: Validate password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);

            if (!isPasswordValid)
            {
                // ✅ STEP 3: Record failed attempt (Identity auto-locks after 5 attempts)
                await _userManager.AccessFailedAsync(user);

                // Check if account just got locked
                if (await _userManager.IsLockedOutAsync(user))
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    return new UserResultDto
                    {
                        Success = false,
                        IsLockedOut = true,
                        LockoutEnd = lockoutEnd,
                        Errors = new[]
                        {
                            //$"Account locked due to multiple failed attempts. Locked until {lockoutEnd?.LocalDateTime:yyyy-MM-dd HH:mm:ss}."
                            $"Account locked due to multiple failed attempts. Contact your admin "
                        }
                    };
                }

                // Return generic error (don't reveal remaining attempts for security)
                return new UserResultDto
                {
                    Success = false,
                    Errors = new[] { "Invalid email or password" }
                };
            }

            // ✅ STEP 4: Reset failed login count on successful authentication
            await _userManager.ResetAccessFailedCountAsync(user);

            return new UserResultDto { Success = true };
        }

        /// <summary>
        /// Signs out the current user (WebApi context).
        /// NOTE: In WebApi, this is rarely used because JWT tokens are stateless.
        /// Logout is typically handled by:
        /// 1. Client discarding the access token
        /// 2. Server revoking the refresh token in the database
        /// 
        /// This method is included for IUserService interface compatibility.
        /// </summary>
        /// <returns>Completed task</returns>
        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        /// <summary>
        /// Registers a new user account in the ASP.NET Core Identity database (WebApi context).
        /// Validates email/username/phone uniqueness, creates the user, and assigns a role.
        /// 
        /// VALIDATION PERFORMED:
        /// - Password and ConfirmPassword match
        /// - Email is not already registered
        /// - Username is unique
        /// - Phone number is unique (if provided)
        /// 
        /// IMPORTANT: Unlike UI's RegisterAsync, this version does NOT sign in the user afterward,
        /// because WebApi uses JWT tokens (not cookies). The user must log in after registration
        /// to obtain tokens.
        /// </summary>
        /// <param name="userDto">User registration data including email, password, name, phone, and role</param>
        /// <returns>UserResultDto with success/failure and detailed validation errors</returns>
        public async Task<UserResultDto> RegisterAsync(UserDto userDto)
        {

            var errors = new List<string>();

            // ✅ STEP 1: Validate ConfirmPassword
            if (string.IsNullOrEmpty(userDto.Password))
            {
                errors.Add("Password is required");
            }

            if (string.IsNullOrEmpty(userDto.ConfirmPassword))
            {
                errors.Add("Confirm password is required");
            }

            if (userDto.Password != userDto.ConfirmPassword)
            {
                errors.Add("Password and confirmation password do not match");
            }

            // ✅ STEP 2: Check if email already exists
            var existingEmail = await _userManager.FindByEmailAsync(userDto.Email);
            if (existingEmail != null)
            {
                errors.Add("Email is already in use");
            }

            // ✅ STEP 3: Check if username exists (if different from email)
            var existingUserName = await _userManager.FindByNameAsync(userDto.Email);
            if (existingUserName != null && existingUserName.Id != existingEmail?.Id)
            {
                errors.Add("Username is already in use");
            }

            // ✅ STEP 4: Check if phone exists (if provided)
            if (!string.IsNullOrEmpty(userDto.Phone))
            {
                var existingPhone = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Phone == userDto.Phone);
                if (existingPhone != null)
                {
                    errors.Add("Phone number is already in use");
                }
            }
            // ✅ Return early if validation failed
            if (errors.Any())
            {
                return new UserResultDto
                {
                    Success = false,
                    Errors = errors
                };
            }
            try
            {
                _logger.LogInformation("Attempting to register user: {Email}", userDto.Email);

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(userDto.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already exists", userDto.Email);
                    return new UserResultDto
                    {
                        Success = false,
                        Errors = new List<string> { "Email address is already registered" }
                    };
                }

                // ✅ STEP 5: Create user with all properties
                var user = new ApplicationUser
                {
                    UserName = userDto.Email,
                    Email = userDto.Email,
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    Phone = userDto.Phone,
                    EmailConfirmed = false // ✅ Require email confirmation (set to true if not using email)
                };
                // Create user with password
                var result = await _userManager.CreateAsync(user, userDto.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ User created successfully: {Email}", userDto.Email);

                    // Assign default role
                    await _userManager.AddToRoleAsync(user, "User");
                    _logger.LogInformation("✅ User {Email} assigned to 'User' role", userDto.Email);

                    // ❌ REMOVED: await _signInManager.SignInAsync(user, isPersistent: false);
                    // ✅ WebApi doesn't sign users in - they must login to get JWT tokens

                    return new UserResultDto
                    {
                        Success = true,
                        Errors = new List<string> { "Registration successful. Please login." }
                        
                    };
                }

                // Registration failed
                _logger.LogWarning("Registration failed for {Email}: {Errors}",
                    userDto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
               
                return new UserResultDto
                {
                    Success = false,
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during registration for {Email}", userDto.Email);
                return new UserResultDto
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred during registration. Please try again." }
                };
               
            }
        }

        

        /// <summary>
        /// Retrieves a user by ID from the Identity database (WebApi context).
        /// Returns full user information including roles.
        /// Used by API endpoints that need user data for authorization/display.
        /// </summary>
        /// <param name="userid">User's ID as a string (GUID format)</param>
        /// <returns>UserDto with complete user details and primary role, or null if user not found</returns>
        public async Task<UserDto> GetUserByIdAsync(string userid)
        {
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = Guid.Parse(user.Id),
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "User"
            };
        }

        /// <summary>
        /// Retrieves all registered users from the database (WebApi context).
        /// Returns full user information including roles and lockout status.
        /// Used for admin endpoints that need user lists.
        /// </summary>
        /// <returns>Collection of UserDto objects containing complete user details</returns>
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLocked = await _userManager.IsLockedOutAsync(user);

                userDtos.Add(new UserDto
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
                    IsLockedOut = isLocked
                });
            }

            return userDtos;
        }

        /// <summary>
        /// Gets the currently authenticated user's ID from JWT token claims.
        /// Extracts the NameIdentifier claim from the validated JWT access token.
        /// 
        /// IMPORTANT DIFFERENCE from UI version:
        /// - UI version: Reads cookie claims (throws exception if missing)
        /// - WebApi version: Reads JWT claims (returns Guid.Empty if missing to avoid breaking API calls)
        /// 
        /// SECURITY NOTE: This method is more lenient than UI's version because some API
        /// endpoints may be called without authentication (e.g., public data endpoints).
        /// Calling code should still verify user is authenticated before using the returned GUID.
        /// </summary>
        /// <returns>User's GUID from JWT claims, or Guid.Empty if not authenticated</returns>
        public Guid GetLoggedInUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userIdClaim = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            // If the JWT was validated and contains the user id claim, return it
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userGuid))
            {
                return userGuid;
            }

            // Optional fallback: explicitly read a refresh token (cookie or header) and resolve the user
            var refreshToken = httpContext?.Request?.Cookies["RefreshToken"]
                               ?? httpContext?.Request?.Headers["X-Refresh-Token"].FirstOrDefault();

            if (!string.IsNullOrEmpty(refreshToken) && _refreshTokenRetriver != null)
            {
                // Await the task to get the actual RefreshTokenDto object
                var refreshTokenDataTask = _refreshTokenRetriver.GetByToken(refreshToken);
                var refreshTokenData = refreshTokenDataTask is Task<RefreshTokenDto> task
                    ? task.GetAwaiter().GetResult()
                    : null;

                if (refreshTokenData != null && Guid.TryParse(refreshTokenData.UserId, out var refreshUserGuid))
                    return refreshUserGuid;
            }

            // No authenticated user found — caller should map this to 401 Unauthorized
            throw new InvalidOperationException("User ID not found in claims. User might not be authenticated.");
        }

        /// <summary>
        /// Gets the currently authenticated user's ID from JWT token claims (async version).
        /// Extracts the NameIdentifier claim from the validated JWT access token.
        /// 
        /// IMPORTANT: Throws InvalidOperationException if user is not authenticated.
        /// This is consistent with the UI version's behavior for security-critical flows.
        /// </summary>
        /// <returns>User's GUID from JWT claims</returns>
        /// <exception cref="InvalidOperationException">Thrown if user ID claim is missing (user not authenticated)</exception>
        public async Task<Guid> GetLoggedInUserAsync()
        {
            var refreshToken = _httpContextAccessor.HttpContext?.Request?.Cookies["RefreshToken"];

            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if userIdString is null or empty before attempting to parse
            if (string.IsNullOrEmpty(userIdString))
            {
                // Throw exception for security-critical flows (consistent with UI version)
                throw new InvalidOperationException("User ID not found in claims. User might not be authenticated.");
            }

            return Guid.Parse(userIdString);
        }

        /// <summary>
        /// Retrieves a user by email address (WebApi context).
        /// Returns minimal user information (ID and email only).
        /// Used by authentication endpoints and password reset flows.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>UserDto with ID and email, or null if user not found</returns>
        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            return new UserDto
            {
                Id = Guid.Parse(user.Id),
                Email = user.Email,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Phone = user.Phone ?? string.Empty,
                NotifyByEmail = user.NotifyByEmail,
                NotifyBySms = user.NotifyBySms,
                NotifyShipmentStatusUpdates = user.NotifyShipmentStatusUpdates,
                NotifyMarketing = user.NotifyMarketing,
                DefaultCountryId = user.DefaultCountryId,
                DefaultCityId = user.DefaultCityId,
                DefaultCarrierId = user.DefaultCarrierId,
                DefaultShippingPackageId = user.DefaultShippingPackageId,
                DefaultShippingTypeId = user.DefaultShippingTypeId,
                
            };
        }

        /// <summary>
        /// Finds a user by email and returns the full ApplicationUser entity (WebApi context).
        /// Used in password reset flows and scenarios requiring direct Identity user access.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>ApplicationUser entity, or null if not found</returns>
        public async Task<ApplicationUser> FindByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        /// <summary>
        /// Generates a password reset token for the specified user (WebApi context).
        /// Token is URL-safe, time-limited, and single-use for security.
        /// Typically sent to user's email for password reset verification.
        /// </summary>
        /// <param name="user">The user requesting password reset</param>
        /// <returns>URL-safe password reset token string</returns>
        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
        {
            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        /// <summary>
        /// Resets a user's password using a valid reset token (WebApi context).
        /// The token must have been generated via GeneratePasswordResetTokenAsync and not yet used/expired.
        /// 
        /// SECURITY: Returns generic "Unable to reset password" message when user is not found
        /// to prevent username enumeration attacks.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="token">Password reset token (from email link)</param>
        /// <param name="newPassword">New password (must meet Identity password requirements)</param>
        /// <returns>IdentityResult with success/failure and validation errors</returns>
        public async Task<IdentityResult> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Return a generic failed result to avoid revealing whether the email exists
                return IdentityResult.Failed(new IdentityError { Description = "Unable to reset password." });
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result;
        }

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
        public async Task<IdentityResult> ChangePasswordAsync(string email, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Return a generic failed result to avoid revealing whether the email exists
                return IdentityResult.Failed(new IdentityError { Description = "Unable to change password." });
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result;
        }

        /// <summary>
        /// Updates an existing user's information (admin operation - WebApi context).
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
            user.UserName = userDto.Email;
            user.NotifyByEmail = userDto.NotifyByEmail;
            user.NotifyBySms = userDto.NotifyBySms;
            user.NotifyShipmentStatusUpdates = userDto.NotifyShipmentStatusUpdates;
            user.NotifyMarketing = userDto.NotifyMarketing;
            user.DefaultCountryId = userDto.DefaultCountryId;
            user.DefaultCityId = userDto.DefaultCityId;
            user.DefaultCarrierId = userDto.DefaultCarrierId;
            user.DefaultShippingPackageId = userDto.DefaultShippingPackageId;
            user.DefaultShippingTypeId = userDto.DefaultShippingTypeId;
           

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

        /// <summary>
        /// Locks a user account, preventing them from logging in (WebApi context).
        /// Sets LockoutEnd to 100 years in the future (effectively permanent).
        /// Used by administrators to disable problematic or inactive accounts.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <returns>IdentityResult with success/failure</returns>
        public async Task<IdentityResult> LockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            var lockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

            if (result.Succeeded)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
            }

            return result;
        }

        /// <summary>
        /// Unlocks a user account, allowing them to log in again (WebApi context).
        /// Clears the LockoutEnd date, restoring normal access.
        /// Used by administrators to re-enable previously locked accounts.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <returns>IdentityResult with success/failure</returns>
        public async Task<IdentityResult> UnlockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            return result;
        }

        /// <summary>
        /// Changes a user's role assignment (WebApi context).
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


    }
}


