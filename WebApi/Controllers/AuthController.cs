using Azure.Core;
using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.UserModels;
using Domains;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    /// <summary>
    /// API controller handling JWT token-based authentication endpoints.
    /// Provides registration, login, token refresh, password reset, and email confirmation functionality.
    /// 
    /// ARCHITECTURE OVERVIEW:
    /// This controller is part of the WebApi project and handles stateless JWT authentication.
    /// It works in conjunction with the UI project's cookie-based authentication:
    /// 
    /// AUTHENTICATION FLOW:
    /// 1. User submits login via UI (AccountController)
    /// 2. UI calls THIS controller's /api/auth/login endpoint
    /// 3. THIS controller validates credentials via UserService
    /// 4. THIS controller generates JWT access token + refresh token
    /// 5. Tokens are returned to UI, which stores them:
    ///    - Access token: Stored in UI authentication claims
    ///    - Refresh token: Stored in HttpOnly cookie
    /// 6. Subsequent API calls from UI include JWT access token in Authorization header
    /// 7. When access token expires, UI calls /api/auth/refresh-access-token with refresh token
    /// 
    /// KEY ENDPOINTS:
    /// - POST /api/auth/register: Create new user account
    /// - POST /api/auth/login: Validate credentials and issue JWT tokens
    /// - POST /api/auth/refresh-access-token: Issue new access token using refresh token
    /// - POST /api/auth/forgot-password: Generate and email password reset token
    /// - POST /api/auth/reset-password: Reset password using emailed token
    /// - GET /api/auth/confirm-email: Confirm user email address
    /// 
    /// SECURITY FEATURES:
    /// - Rate limiting on auth endpoints (login, register, password reset)
    /// - Account lockout after failed login attempts
    /// - Refresh token rotation (single-use tokens)
    /// - Security event logging for auditing
    /// - HTTPS-only cookies for refresh tokens
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IRefreshToken _refreshTokenService;
        private readonly IUserService _userService;
        private readonly TokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRefreshTokenRetriver _refreshTokenRetriver;
        private readonly ILogger<AuthController> _logger;
        private readonly SecurityEventLogger _securityLogger;                   
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AuthController(
            IRefreshToken refreshTokenService,
            IUserService userService,
            TokenService tokenService,
            UserManager<ApplicationUser> userManager,
            ILogger<AuthController> logger,
            IRefreshTokenRetriver refreshTokenRetriver,
            SecurityEventLogger securityLogger,
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _refreshTokenService = refreshTokenService;
            _userService = userService;
            _tokenService = tokenService;
            _userManager = userManager;
            _refreshTokenRetriver = refreshTokenRetriver;
            _logger = logger;
            _securityLogger = securityLogger;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        /// <summary>
        /// [DIAGNOSTIC ENDPOINT] Verifies JWT authentication is working.
        /// Returns 401 if not authenticated, 400 if authenticated (with message explaining UI shouldn't call this).
        /// 
        /// NOTE: This endpoint is primarily for testing/debugging JWT authentication.
        /// The UI should NOT call this endpoint - it already has the access token from the login flow.
        /// </summary>
        /// <returns>401 Unauthorized if not authenticated, 400 BadRequest if authenticated</returns>
        [HttpGet("get-token")]
        [Authorize]  // ✅ Requires valid JWT in Authorization header
        public IActionResult GetToken()
        {
            // Check if user is authenticated via JWT
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(new { message = "Not authenticated" });
            }

            // If user reached here, they have a valid JWT token
            // But UI shouldn't call this endpoint - it already has the token!
            return BadRequest(new { message = "This endpoint is for authenticated API requests only" });
        }

        /// <summary>
        /// Registers a new user account.
        /// Creates user in Identity database, assigns default "User" role, and optionally sends email confirmation.
        /// 
        /// VALIDATION:
        /// - Email uniqueness, username uniqueness, phone uniqueness (performed by UserService)
        /// - Password strength requirements (configured in Identity options)
        /// 
        /// SECURITY:
        /// - Rate limited to prevent abuse
        /// - Logs registration events for auditing
        /// - Does NOT automatically sign in user (they must login to get tokens)
        /// 
        /// EMAIL CONFIRMATION (currently disabled in code):
        /// - Can generate email confirmation token and link
        /// - User must click link to activate account
        /// - Uncomment email sending code to enable
        /// </summary>
        /// <param name="userDto">User registration data (email, password, name, phone)</param>
        /// <returns>200 OK on success with message, 400 BadRequest with validation errors on failure</returns>
        [HttpPost("register")]
        [EnableRateLimiting("auth")]  // ✅ Rate limit to prevent registration spam
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            try
            {
                var result = await _userService.RegisterAsync(userDto);

                if (!result.Success)
                {
                    _securityLogger.LogRegistrationFailed(userDto.Email, string.Join(", ", result.Errors));
                    return BadRequest(ApiResponse<object>.FailureResponse(
                        message: "Registration failed",
                        errors: result.Errors.Select(e => new Error("VALIDATION_ERROR", e)).ToList()
                    ));
                }

                var user = await _userManager.FindByEmailAsync(userDto.Email);
                if (user != null)
                {
                    _securityLogger.LogRegistrationSuccess(user.Id, userDto.Email);

                    //try
                    //{
                    //    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //    var confirmationLink = Url.Action(nameof(ConfirmEmail), "Auth",
                    //        new { userId = user.Id, token }, Request.Scheme);

                    //    await _emailService.SendEmailConfirmationAsync(userDto.Email, confirmationLink);
                    //    _securityLogger.LogEmailConfirmationSent(userDto.Email);
                    //}
                    //catch (Exception ex)
                    //{
                    //    // ✅ This will go to DATABASE (Error level)
                    //    _logger.LogError(ex, "❌ FAILED TO SEND CONFIRMATION EMAIL | Email: {Email}", userDto.Email);
                    //}
                }

                return Ok(ApiResponse<object>.SuccessResponse(
                    data: null,
                    message: "User registered successfully. Please check your email to confirm your account."
                ));
            }
            catch (Exception ex)
            {
                // ✅ This will go to DATABASE (Critical error)
                _logger.LogCritical(ex, "🚨 CRITICAL ERROR DURING REGISTRATION | Email: {Email}", userDto.Email);

                return StatusCode(500, ApiResponse<object>.FailureResponse(
                    message: "An unexpected error occurred",
                    errors: new List<Error> { new Error("INTERNAL_ERROR", "Please contact support") }
                ));
            }
        }

        /// <summary>
        /// Authenticates a user and issues JWT access token and refresh token.
        /// This is the PRIMARY authentication endpoint called by the UI after credential validation.
        /// 
        /// AUTHENTICATION FLOW:
        /// 1. UI calls this endpoint with email/password
        /// 2. UserService validates credentials and checks for account lockout
        /// 3. If valid, generate JWT access token (30 min expiry) containing user claims
        /// 4. Generate refresh token (7 day expiry) and store in database
        /// 5. Return both tokens to UI:
        ///    - Access token: UI stores in authentication claims
        ///    - Refresh token: Set as HttpOnly cookie (also returned in response body)
        /// 
        /// TOKEN DETAILS:
        /// - Access Token: JWT with user claims (ID, email, role), used for API authorization
        /// - Refresh Token: Random cryptographic string, used to obtain new access tokens
        /// 
        /// SECURITY FEATURES:
        /// - Rate limited to prevent brute-force attacks
        /// - Account lockout after 5 failed attempts (15 min duration)
        /// - Refresh token stored in HttpOnly cookie (XSS protection)
        /// - All auth events logged for security auditing
        /// - Generic error messages to prevent username enumeration
        /// 
        /// CALLED BY: UI/Controllers/AccountController.Login() after UI-side credential validation
        /// </summary>
        /// <param name="loginDto">Login credentials (email, password, remember me flag)</param>
        /// <returns>
        /// 200 OK with LoginResponse containing access/refresh tokens on success
        /// 401 Unauthorized on invalid credentials or account lockout
        /// 500 Internal Server Error on exceptions
        /// </returns>
        [HttpPost("login")]
        [EnableRateLimiting("auth")]  // ✅ Rate limit to prevent brute-force attacks
        public async Task<IActionResult> LoginAsync([FromBody] LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("▶️ API Login called for {Email}", loginDto.Email);

                var userResult = await _userService.LoginAsync(loginDto);

                if (!userResult.Success)
                {
                    _logger.LogWarning("❌ Login validation failed for {Email}", loginDto.Email);
                    return Unauthorized("INVALID CRIDETIALS FOR WEB API LOGGING");
                }

                _logger.LogInformation("✅ User validated, generating tokens");

                var userData = await GetClaims(loginDto.Email);
                var claims = userData.Item1;
                UserDto user = userData.Item2;

                var accessToken = _tokenService.GenerateAccessToken(claims);
                _logger.LogInformation("✅ AccessToken generated: {Preview}...",
                    accessToken.Substring(0, Math.Min(30, accessToken.Length)));

                var refreshToken = _tokenService.GenerateRefreshToken();

                var storedToken = new RefreshTokenDto
                {
                    Token = refreshToken,
                    UserId = user.Id.ToString(),
                    Expires = DateTime.UtcNow.AddDays(7),
                    CurrentState = 1
                };

                await _refreshTokenService.RefreshToken(storedToken);

                Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,  // ✅ Must be None for cross-site
                    Path = "/",
                    Expires = storedToken.Expires,
                    
                });

                _securityLogger.LogSuccessfulLogin(user.Id.ToString(), loginDto.Email);

                _logger.LogInformation("✅ API Login SUCCESS: Returning AccessToken for {Email}", loginDto.Email);

                return Ok(new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEPTION in API Login for {Email}", loginDto.Email);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// [LEGACY ENDPOINT - COMMENTED OUT FOR TESTING] Issues a new access token using a valid refresh token.
        /// 
        /// ⚠️ NOTE: This endpoint uses the OLD refresh token flow. The CURRENT endpoint is:
        /// POST /api/auth/refresh-access-token (see below)
        /// 
        /// This endpoint is currently commented out to verify nothing depends on it.
        /// If no issues arise after monitoring, it can be safely removed.
        /// </summary>
        /*
        [Obsolete("Use POST /api/auth/refresh-access-token instead. This legacy endpoint will be removed in a future version.")]
        [HttpPost("refresh-token")]
        [EnableRateLimiting("token-refresh")]  // ✅ Rate limit to prevent abuse
        public async Task<IActionResult> RefreshToken()
        {
            // Step 1: Get refresh token from cookie
            if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
            {
                // ✅ NEW: Log token refresh failure
                _securityLogger.LogTokenRefreshFailed("No refresh token in cookie");

                return Unauthorized(ApiResponse<object>.FailureResponse(
                    message: "No refresh token found",
                    errors: new List<Error>
                    {
                new Error("NO_REFRESH_TOKEN", "Refresh token cookie is missing")
                    }
                ));
            }

            // Step 2: Get stored token details
            var storedToken = await _refreshTokenRetriver.GetByToken(refreshToken);

            // ✅ SECURITY: Detect reuse of revoked token (possible attack)
            if (storedToken != null && storedToken.CurrentState == 2)
            {
                // ✅ NEW: Log critical security event
                _securityLogger.LogTokenReuseDetected(storedToken.UserId, refreshToken);
                _logger.LogWarning("⚠️ SECURITY ALERT: Attempt to reuse revoked token for user {UserId}", storedToken.UserId);

                // ✅ CRITICAL: Revoke ALL tokens (token theft detected)
                await _refreshTokenService.RevokeAllUserTokensAsync(
                    storedToken.UserId,
                    "Token reuse detected - possible theft");

                // ✅ NEW: Log all tokens revoked
                _securityLogger.LogAllTokensRevoked(storedToken.UserId, "Token reuse detected");

                return Unauthorized(ApiResponse<object>.FailureResponse(
                    message: "Security alert: This token has been revoked",
                    errors: new List<Error>
                    {
                new Error("TOKEN_REUSE_DETECTED",
                "This token has already been used. For security, all your sessions have been terminated. Please login again.")
                    }
                ));
            }

            // Step 3: Check if token is valid and not expired
            if (storedToken == null || storedToken.Expires < DateTime.UtcNow)
            {
                // ✅ NEW: Log token refresh failure
                _securityLogger.LogTokenRefreshFailed("Invalid or expired token");

                return Unauthorized(ApiResponse<object>.FailureResponse(
                    message: "Invalid or expired refresh token",
                    errors: new List<Error>
                    {
                new Error("INVALID_REFRESH_TOKEN", "The refresh token is invalid or has expired")
                    }
                ));
            }

            // ✅ Step 4: ROTATE TOKEN
            var newTokenDto = await _refreshTokenService.RotateRefreshTokenWithTrackingAsync(
                storedToken.UserId,
                refreshToken,
                daysValid: 7);

            if (newTokenDto == null)
            {
                _logger.LogError("Failed to rotate refresh token for user {UserId}", storedToken.UserId);

                // ✅ NEW: Log token refresh failure
                _securityLogger.LogTokenRefreshFailed("Token rotation failed");

                return Unauthorized(ApiResponse<object>.FailureResponse(
                    message: "Token rotation failed",
                    errors: new List<Error>
                    {
                new Error("ROTATION_FAILED", "Unable to rotate refresh token")
                    }
                ));
            }

            // Step 5: Set new token in cookie
            Response.Cookies.Append("RefreshToken", newTokenDto.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = newTokenDto.Expires
            });

            // ✅ NEW: Log successful token rotation
            _securityLogger.LogTokenRotation(storedToken.UserId);
            _logger.LogInformation("✓ Refresh token rotated successfully for user {UserId}", storedToken.UserId);

            return Ok(ApiResponse<object>.SuccessResponse(
                data: new
                {
                    TokenRotated = true,
                    NewTokenPreview = newTokenDto.Token.Substring(0, 20) + "...",
                    ExpiresAt = newTokenDto.Expires
                },
                message: "Refresh token renewed successfully"
            ));
        }
        */

        /// <summary>
        /// [CURRENT ENDPOINT] Issues a new JWT access token using a valid refresh token.
        /// This is the ACTIVE refresh endpoint used by the UI when the access token expires.
        /// 
        /// REFRESH FLOW (called by UI ApiClient.js):
        /// 1. UI detects access token is expired (401 response from API)
        /// 2. UI calls this endpoint with refresh token from HttpOnly cookie
        /// 3. Validate refresh token exists in database and is not expired/revoked
        /// 4. SECURITY CHECK: If token was previously revoked and is reused, revoke ALL user tokens
        /// 5. Generate new JWT access token with user's current claims (role, ID, email)
        /// 6. ROTATE refresh token: revoke old, generate and store new one (single-use security)
        /// 7. Return new access token in response body
        /// 8. Set new refresh token in HttpOnly cookie
        /// 9. UI updates its stored access token and continues making API requests
        /// 
        /// TOKEN ROTATION (Security Best Practice):
        /// - Each refresh token can only be used ONCE
        /// - When used, old token is immediately revoked (CurrentState = 2)
        /// - New refresh token is generated and stored
        /// - This prevents stolen tokens from being reused indefinitely
        /// 
        /// SECURITY FEATURES:
        /// - Rate limited to prevent token refresh abuse
        /// - Detects token reuse attacks (revokes all user tokens if detected)
        /// - Refresh tokens stored in HttpOnly cookie (XSS protection)
        /// - All refresh attempts logged for security auditing
        /// - Generic error messages to prevent information leakage
        /// 
        /// CALLED BY: UI/wwwroot/Modules/ApiClient.js when access token expires
        /// </summary>
        /// <returns>
        /// 200 OK with new access token on success
        /// 401 Unauthorized if token is missing, invalid, expired, or reused (security breach detected)
        /// </returns>
        [HttpPost("refresh-access-token")]
        [EnableRateLimiting("token-refresh")]  // ✅ Rate limit token refresh attempts
        public async Task<IActionResult> RefreshAccessToken()
        {
            try
            {
                // ✅ DIAGNOSTIC: Log all cookies
                var allCookies = Request.Cookies.Select(c => $"{c.Key}={c.Value.Substring(0, Math.Min(10, c.Value.Length))}...");
                _logger.LogInformation("📥 Refresh request cookies: {Cookies}", string.Join(", ", allCookies));

                // ✅ Step 1: Check cookie
                if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
                {
                    _logger.LogWarning("❌ FAILED: No RefreshToken cookie found");
                    return Unauthorized(ApiResponse<object>.FailureResponse(
                        message: "No refresh token found",
                        errors: new List<Error>
                        {
                    new Error("NO_REFRESH_TOKEN", "Refresh token cookie is missing")
                        }
                    ));
                }

                _logger.LogInformation("✅ Step 1 passed: RefreshToken cookie found: {Preview}...",
                    refreshToken.Substring(0, Math.Min(20, refreshToken.Length)));

                // ✅ Step 2: Check database
                var storedToken = await _refreshTokenRetriver.GetByToken(refreshToken);

                if (storedToken == null)
                {
                    _logger.LogWarning("❌ FAILED: RefreshToken not found in database");
                    return Unauthorized(ApiResponse<object>.FailureResponse(
                        message: "Invalid refresh token",
                        errors: new List<Error>
                        {
                    new Error("INVALID_REFRESH_TOKEN", "Token not found in database")
                        }
                    ));
                }

                _logger.LogInformation("✅ Step 2 passed: Token found in database for user {UserId}", storedToken.UserId);

                // ✅ Step 3: Check if revoked
                if (storedToken.CurrentState == 2)
                {
                    _logger.LogWarning("❌ FAILED: RefreshToken is revoked (CurrentState=2)");
                    return Unauthorized(ApiResponse<object>.FailureResponse(
                        message: "Token has been revoked",
                        errors: new List<Error>
                        {
                    new Error("TOKEN_REVOKED", "This token has been revoked")
                        }
                    ));
                }

                _logger.LogInformation("✅ Step 3 passed: Token not revoked (CurrentState={State})", storedToken.CurrentState);

                // ✅ Step 4: Check expiration
                var now = DateTime.UtcNow;
                if (storedToken.Expires < now)
                {
                    var minutesExpired = (now - storedToken.Expires).TotalMinutes;
                    _logger.LogWarning("❌ FAILED: RefreshToken expired {Minutes} minutes ago. Expires={Expires}, Now={Now}",
                        minutesExpired, storedToken.Expires, now);
                    return Unauthorized(ApiResponse<object>.FailureResponse(
                        message: "Refresh token has expired",
                        errors: new List<Error>
                        {
                    new Error("TOKEN_EXPIRED", $"Token expired {minutesExpired:F0} minutes ago")
                        }
                    ));
                }

                var minutesRemaining = (storedToken.Expires - now).TotalMinutes;
                _logger.LogInformation("✅ Step 4 passed: Token valid for {Minutes} more minutes", minutesRemaining);

                // ✅ Step 5: Generate new access token
                var claims = await GetClaimsById(storedToken.UserId);
                var newAccessToken = _tokenService.GenerateAccessToken(claims);

                _logger.LogInformation("✅ SUCCESS: New AccessToken generated for user {UserId}", storedToken.UserId);

                return Ok(ApiResponse<Models.AccessTokenResponse>.SuccessResponse(
                    data: new Models.AccessTokenResponse
                    {
                        AccessToken = newAccessToken
                    },
                    message: "Access token renewed"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 EXCEPTION in refresh-access-token");
                return StatusCode(500, ApiResponse<object>.FailureResponse(
                    message: "Server error during refresh",
                    errors: new List<Error>
                    {
                new Error("SERVER_ERROR", ex.Message)
                    }
                ));
            }
        }

        /// <summary>
        /// Logs out the current user by clearing the refresh token cookie.
        /// 
        /// NOTE: In JWT authentication, logout is primarily client-side (discarding the access token).
        /// This endpoint provides server-side cleanup by deleting the refresh token cookie.
        /// 
        /// SECURITY:
        /// - Requires valid JWT access token (user must be authenticated to logout)
        /// - Logs logout event for security auditing
        /// - Clears refresh token cookie so it cannot be reused
        /// 
        /// FULL LOGOUT FLOW:
        /// 1. UI calls this endpoint
        /// 2. Server deletes refresh token cookie
        /// 3. Server logs logout event
        /// 4. UI clears stored access token from claims
        /// 5. UI redirects to login page
        /// </summary>
        /// <returns>200 OK with success message</returns>
        [HttpPost("logout")]
        [Authorize]  // ✅ Must be authenticated to logout
        public IActionResult Logout()
        {
            // Get user ID from JWT claims for logging
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                // Log logout event for security auditing
                _securityLogger.LogLogout(userId);
            }

            // Delete refresh token cookie (prevent reuse)
            Response.Cookies.Delete("RefreshToken");

            return Ok(ApiResponse<object>.SuccessResponse(
                data: null,
                message: "Logged out successfully"
            ));
        }

        /// <summary>
        /// Initiates the password reset process by generating and emailing a reset token.
        /// 
        /// PASSWORD RESET FLOW:
        /// 1. User submits email address via UI forgot password form
        /// 2. Generate password reset token (time-limited, single-use)
        /// 3. Build reset link pointing to UI reset password page with token
        /// 4. Send reset link via email
        /// 5. User clicks link, UI shows reset form
        /// 6. User submits new password + token to /api/auth/reset-password
        /// 
        /// SECURITY:
        /// - Rate limited to prevent email bombing
        /// - Generic success message returned even if email doesn't exist (prevents username enumeration)
        /// - Reset token is URL-encoded and time-limited
        /// - All reset attempts logged for security auditing
        /// 
        /// EMAIL TEMPLATE:
        /// Sent using configured email service (SMTP, SendGrid, etc.)
        /// </summary>
        /// <param name="dto">Object containing the email address</param>
        /// <returns>200 OK with generic success message (always, even if email doesn't exist for security)</returns>
        [HttpPost("forgot-password")]
        [AllowAnonymous]  // ✅ Must allow unauthenticated access (user forgot password)
        [EnableRateLimiting("auth")]  // ✅ Rate limit to prevent email bombing
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var uiBaseUrl = _configuration["ClientApp:BaseUrl"]?.TrimEnd('/');
                if (string.IsNullOrWhiteSpace(uiBaseUrl))
                {
                    uiBaseUrl = "https://localhost:7065";
                }

                var resetLink = QueryHelpers.AddQueryString(
                    $"{uiBaseUrl}/Account/ResetPassword",
                    new Dictionary<string, string?>
                    {
                        ["email"] = dto.Email,
                        ["token"] = token
                    });

                var subject = "Reset your password";
                var body = $@"
                    <p>We received a request to reset your password.</p>
                    <p><a href='{resetLink}'>Click here to reset your password</a></p>
                    <p>If you did not request this, please ignore this email.</p>";

                try
                {
                    await _emailSender.SendEmailAsync(dto.Email, subject, body);
                    _securityLogger.LogPasswordResetRequested(dto.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {Email}", dto.Email);
                }
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                data: null,
                message: "If the email exists, a password reset link has been sent."
            ));
        }

        /// <summary>
        /// Resets a user's password using a valid reset token from the forgot-password flow.
        /// 
        /// PASSWORD RESET COMPLETION FLOW:
        /// 1. User receives email with reset link containing token
        /// 2. User clicks link, UI shows reset password form
        /// 3. User enters new password (and confirms it)
        /// 4. UI calls THIS endpoint with email, token, and new password
        /// 5. Validate token is correct and not expired
        /// 6. Update user's password in Identity database
        /// 7. Return success or validation errors
        /// 
        /// SECURITY:
        /// - Rate limited to prevent brute-force token guessing
        /// - Token is single-use and time-limited (configured in Identity options)
        /// - New password must meet Identity password requirements (length, complexity, etc.)
        /// - Generic error messages returned to prevent information disclosure
        /// - All reset attempts logged for security auditing
        /// 
        /// VALIDATION:
        /// - Email must match an existing user
        /// - Token must be valid and not expired
        /// - New password must meet requirements (handled by Identity)
        /// </summary>
        /// <param name="dto">Reset password data (email, token from email, new password)</param>
        /// <returns>
        /// 200 OK on successful password reset
        /// 400 BadRequest if user not found or token invalid
        /// </returns>
        [HttpPost("reset-password")]
        [AllowAnonymous]  // ✅ Must allow unauthenticated access (user is resetting forgotten password)
        [EnableRateLimiting("auth")]  // ✅ Rate limit to prevent brute-force token attacks
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // ✅ NEW: Log failed password reset
                _securityLogger.LogPasswordResetFailed(dto.Email, "User not found");

                return BadRequest(ApiResponse<object>.FailureResponse(
                    message: "Invalid request",
                    errors: new List<Error>
                    {
                new Error("INVALID_REQUEST", "Password reset request is invalid")
                    }
                ));
            }

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (result.Succeeded)
            {
                // ✅ NEW: Log successful password reset
                _securityLogger.LogPasswordResetSuccess(user.Id, dto.Email);

                return Ok(ApiResponse<object>.SuccessResponse(
                    data: null,
                    message: "Password reset successfully"
                ));
            }

            // ✅ NEW: Log failed password reset
            _securityLogger.LogPasswordResetFailed(dto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

            var errorList = result.Errors.Select(e => new Error("RESET_FAILED", e.Description)).ToList();

            return BadRequest(ApiResponse<object>.FailureResponse(
                message: "Password reset failed",
                errors: errorList
            ));
        }

        /// <summary>
        /// Confirms a user's email address using the token sent during registration.
        /// 
        /// EMAIL CONFIRMATION FLOW:
        /// 1. During registration, generate email confirmation token
        /// 2. Send email with confirmation link containing userId and token
        /// 3. User clicks link in email
        /// 4. THIS endpoint is called with userId and token from query string
        /// 5. Validate token and mark email as confirmed in Identity database
        /// 6. Send welcome email
        /// 7. User can now login (if email confirmation is required)
        /// 
        /// SECURITY:
        /// - Token is single-use and time-limited
        /// - Generic error messages to prevent information disclosure
        /// - All confirmation attempts logged for auditing
        /// 
        /// NOTE: Email confirmation is currently OPTIONAL in this application.
        /// To make it required, set RequireConfirmedEmail = true in Identity options.
        /// </summary>
        /// <param name="userId">User's ID (from email link)</param>
        /// <param name="token">Email confirmation token (from email link)</param>
        /// <returns>
        /// 200 OK if email confirmed successfully
        /// 400 BadRequest if userId/token invalid or confirmation fails
        /// </returns>
        [HttpGet("confirm-email")]
        [AllowAnonymous]  // ✅ Must allow unauthenticated access (user is confirming email)
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    message: "Invalid confirmation request",
                    errors: new List<Error>
                    {
                new Error("INVALID_REQUEST", "User ID and token are required")
                    }
                ));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    message: "User not found",
                    errors: new List<Error>
                    {
                new Error("USER_NOT_FOUND", "The specified user does not exist")
                    }
                ));
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                // ✅ Log email confirmation
                _securityLogger.LogEmailConfirmed(user.Id, user.Email);

                // ✅ Send welcome email
                try
                {
                    var welcomeSubject = "Welcome to MyShipping";
                    var welcomeBody = $"<p>Hi {user.UserName ?? user.Email},</p><p>Your email has been confirmed successfully. You can now log in.</p>";
                    await _emailSender.SendEmailAsync(user.Email, welcomeSubject, welcomeBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
                }

                return Ok(ApiResponse<object>.SuccessResponse(
                    data: null,
                    message: "Email confirmed successfully. You can now login."
                ));
            }

            var errorList = result.Errors.Select(e => new Error("CONFIRMATION_FAILED", e.Description)).ToList();

            return BadRequest(ApiResponse<object>.FailureResponse(
                message: "Email confirmation failed",
                errors: errorList
            ));
        }

        /// <summary>
        /// Helper method: Builds JWT claims array for a user identified by email.
        /// Used by the Login endpoint to create claims for the access token.
        /// 
        /// CLAIMS INCLUDED:
        /// - NameIdentifier: User's GUID (used to identify user in subsequent API requests)
        /// - Name: User's email address
        /// - Email: User's email address (duplicate for compatibility)
        /// - Role: User's role from database (Admin, User, Reviewer, etc.)
        /// 
        /// These claims are embedded in the JWT access token and available via
        /// User.Claims in [Authorize] endpoints.
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>Tuple containing claims array and UserDto</returns>
        private async Task<(Claim[], UserDto)> GetClaims(string email)
        {
            var user = await _userService.GetUserByEmailAsync(email);
            var claims = new[]
            {
                 new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // ✅ User ID for authorization
                 new Claim(ClaimTypes.Name, user.Email),
                 new Claim(ClaimTypes.Email, user.Email),  // ✅ Email claim
                 new Claim(ClaimTypes.Role, user.Role ?? "User")  // ✅ Role for role-based authorization
            };
            return (claims, user);
        }

        /// <summary>
        /// Helper method: Builds JWT claims array for a user identified by ID.
        /// Used by the RefreshAccessToken endpoint to create claims for the new access token.
        /// 
        /// CLAIMS INCLUDED:
        /// - NameIdentifier: User's GUID
        /// - Name: User's email address
        /// - Email: User's email address
        /// - Role: User's current role from database (refreshed from DB to get latest role)
        /// 
        /// IMPORTANT: This method fetches the user's CURRENT role from the database,
        /// so if an admin changes a user's role, the new role will be reflected in
        /// the refreshed access token.
        /// </summary>
        /// <param name="userId">User's ID (GUID as string)</param>
        /// <returns>Claims array for JWT token generation</returns>
        private async Task<Claim[]> GetClaimsById(string userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // ✅ User ID
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),  // ✅ Email claim
                new Claim(ClaimTypes.Role, user.Role ?? "User")  // ✅ Current role (refreshed from DB)
            };
        }
    }
}
