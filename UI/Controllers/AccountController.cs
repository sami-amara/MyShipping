using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.UserModels;
using Domains;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Qowaiv.Validation.Abstractions;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using UI.Helpers;
using UI.Models;
using UI.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace UI.Controllers
{
    public class AccountController : Controller
    {
        //private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUIUserService _userService;
        private readonly GenericApiClient _genericApiClient;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;
        private readonly AccountHelper _accountHelper;
        public AccountController(
             IUIUserService userService,
            SignInManager<ApplicationUser> signInManager,
            GenericApiClient genericApiClient,
            IEmailSender emailSender,
            ILogger<AccountController> logger, AccountHelper accountHelper)
        {
           // _userManager = signInManager.UserManager;
            _signInManager = signInManager;
            _userService = userService;
            _genericApiClient = genericApiClient;
            _emailSender = emailSender;
            _logger = logger;
            _accountHelper = accountHelper;
        }

        

        /// <summary>
        /// Returns the JWT access token stored in the user's authentication claims.
        /// Called by client-side JavaScript (ApiClient.js) to obtain the token for WebApi calls.
        /// 
        /// CRITICAL: This endpoint is called by PageEvents.js on document.ready to populate dropdowns.
        /// The fix for the "manual refresh required" issue ensures that SignInAsync() completes
        /// before redirecting, so this endpoint will always find the AccessToken claim on first load.
        /// </summary>
        /// <returns>JSON object containing the access token, or 401 Unauthorized if token is missing</returns>
        [HttpGet]
        [Authorize]  // ✅ Must be authenticated to retrieve token
        public IActionResult GetAccessToken()
        {
            // ✅ Retrieve AccessToken from authenticated user's claims
            // This claim was added during login in the Login() POST action
            var token = User.FindFirst("AccessToken")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("⚠️ GetAccessToken called but AccessToken claim is missing for user {User}",
                    User.Identity?.Name ?? "Unknown");
                return Unauthorized(new { message = "Token not found in claims" });
            }

            _logger.LogInformation("✅ GetAccessToken returning token for {User}", User.Identity?.Name);
            return Ok(new { token });
        }

        // 🔐 GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        //// ═══════════════════════════════════════════════════════════════
        //// 🔐 LOGIN
        //// ═══════════════════════════════════════════════════════════════


        /// <summary>
        /// Handles user login with hybrid authentication:
        /// 1. Validates credentials via UserManager (ASP.NET Core Identity)
        /// 2. Obtains JWT access token from Web API
        /// 3. Signs in user with cookie authentication (for UI)
        /// 4. Stores access token in claims (for API calls from UI)
        /// 
        /// IMPORTANT FIX: Ensures SignInAsync completes BEFORE redirect to prevent 
        /// the "manual refresh required" issue.
        /// </summary>
        /// <param name="model">Login credentials (email, password, remember me)</param>
        /// <param name="returnUrl">URL to redirect after successful login (optional)</param>
        /// <returns>View with errors on failure, or redirect to home/admin dashboard on success</returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
       {
            ViewData["ReturnUrl"] = returnUrl;

            // ✅ STEP 0: Validate model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login failed: Invalid ModelState for {Email}", model.Email);
                return View(model);
            }

            try
            {
                // ✅ STEP 1: Validate credentials via UserService (uses ASP.NET Core Identity)
                // This checks password, handles lockout, and validates the user exists
                var result = await _userService.LoginAsync(model);

                if (!result.Success)
                {
                    if (result.IsLockedOut)
                    {
                        if (result.IsAdminLocked)
                        {
                            _logger.LogWarning("Login failed: Account permanently locked by admin for {Email}", model.Email);
                            TempData["MessageType"] = MessageType.AdminLocked;
                            return View(model);
                        }
                        else
                        {
                            _logger.LogWarning("Login failed: Account temporarily locked out for {Email}", model.Email);
                            //TempData["MessageType"] = MessageType.TempLocked;
                            return View(model);
                        }
                    }

                    _logger.LogWarning("Login failed: Invalid credentials for {Email}", model.Email);
                    //AddErrorsToModelState(result.Errors);
                    _accountHelper.AddErrorsToModelState(ModelState, result.Errors);
                    return View(model);
                }

                // ✅ STEP 2: Obtain JWT access token from Web API
                // UI calls the WebApi /api/auth/login endpoint to get tokens for API operations
                var loginResponse = await _genericApiClient.PostAsync<LoginResponse>("api/auth/login", model);
                if (loginResponse == null || string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    _logger.LogError("Login failed: API returned null response for {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                    return View(model);
                }

                // ✅ Store refresh token in HttpOnly cookie (secure - JavaScript can't access it)
                // Used to obtain new access tokens when the current one expires
                Response.Cookies.Append("RefreshToken", loginResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,                  // ✅ XSS protection - JavaScript can't read it
                    Secure = true,                    // ✅ HTTPS only
                    SameSite = SameSiteMode.None,    // ✅ Allows cross-site (UI → WebApi)
                    Expires = DateTime.UtcNow.AddDays(7) // 7-day lifetime
                });

                // ✅ Step 3: Sign in with cookie authentication
                // Store access token in claims so it's available via User.Claims in subsequent requests
                var dbUser = await _userService.GetUserByEmailAsync(model.Email);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, dbUser.Email),
                    new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
                    new Claim(ClaimTypes.Role, dbUser.Role ?? "User"),
                    new Claim("AccessToken", loginResponse.AccessToken)  // ✅ Token stored in claims
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // ✅ Await SignInAsync to ensure cookie is written before redirect
                await HttpContext.SignInAsync(
                    IdentityConstants.ApplicationScheme,  // ✅ Use Identity's scheme
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,       // Remember me checkbox
                        ExpiresUtc = model.RememberMe
                            ? DateTimeOffset.UtcNow.AddDays(1) // 1 day if remembered
                            : DateTimeOffset.UtcNow.AddHours(20), // 20 hours otherwise
                        AllowRefresh = true,                    // Allow sliding expiration
                        IssuedUtc = DateTimeOffset.UtcNow
                    });

                _logger.LogInformation("✅ LOGIN SUCCESS: User {Email} authenticated with role {Role}", 
                    model.Email, dbUser.Role ?? "User");

                // ✅ STEP 4: Redirect based on role
                // At this point, the authentication cookie is fully committed to the HTTP response.
                // When the browser navigates to the redirect URL, it will include this cookie in the request.
                // The next page load will have full authentication context, allowing JavaScript to call
                // /Account/GetAccessToken successfully and populate dropdowns via ManagePageControlls.
                if (!string.IsNullOrEmpty(dbUser.Role) && dbUser.Role.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Redirecting Admin user {Email} to admin dashboard", model.Email);
                    return RedirectToAction("Index", "Home", new { area = "admin" });
                }

                // ✅ Redirect to returnUrl if valid and local (prevents open redirect vulnerability)
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    _logger.LogInformation("Redirecting user {Email} to return URL: {ReturnUrl}", model.Email, returnUrl);
                    return Redirect(returnUrl);
                }

                // ✅ Default: redirect to home page
                _logger.LogInformation("Redirecting user {Email} to home page", model.Email);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ EXCEPTION during login for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                return View(model);
            }
       }

        /// <summary>
        /// GET: Displays the logout confirmation view.
        /// User must confirm logout by clicking a button that POSTs to LogoutConfirmed.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            return View();  // Shows confirmation view
        }

        /// <summary>
        /// POST: Performs the actual logout operation.
        /// Clears authentication cookies, signs out the user, and redirects to home.
        /// Protected with [ValidateAntiForgeryToken] to prevent CSRF attacks.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]  // ✅ CSRF protection - requires valid anti-forgery token
        [Authorize]  // ✅ Must be authenticated to logout
        public async Task<IActionResult> LogoutConfirmed()
        {
            var userName = User?.Identity?.Name ?? "Unknown";

            try
            {
                // ✅ STEP 1: Clear authentication cookies
                // Remove access and refresh tokens from browser
                Response.Cookies.Delete("AccessToken");
                Response.Cookies.Delete("RefreshToken");

                // ✅ STEP 2: Sign out using ASP.NET Core Identity
                // This removes the authentication cookie and clears the user's claims principal
                await _userService.LogoutAsync();

                _logger.LogInformation("✅ LOGOUT SUCCESS: User {UserName} logged out", userName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR during logout for user {UserName}", userName);
            }

            return RedirectToAction("Index", "Home");
        }

        
        // ═══════════════════════════════════════════════════════════════
        // 🔓 REGISTER
        // ═══════════════════════════════════════════════════════════════
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }


        // 🔓 POST: /Account/Register


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]  // ✅ Same strict policy for registration
        public async Task<IActionResult> Register(UserDto user)
        {
            TempData["MessageType"] = null;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Registration failed: Invalid ModelState for {Email}", user.Email);
                return View(user);
            }

            try
            {
                var result = await _userService.RegisterAsync(user);

                if (result.Success)
                {
                    _logger.LogInformation("✅ REGISTRATION SUCCESS: User {Email} registered successfully", user.Email);
                    TempData["MessageType"] = MessageType.RegistrationSuccess;
                    TempData["SuccessMessage"] = "Registration successful! Please log in.";
                    return RedirectToAction(nameof(Login));
                }

                _logger.LogWarning("Registration failed for {Email}: {Errors}",
                    user.Email, string.Join(", ", result.Errors ?? new List<string>()));

                //_accountHelper.AddErrorsToModelState(result.Errors);
                _accountHelper.AddErrorsToModelState(ModelState, result.Errors);
                TempData["MessageType"] = MessageType.RegistrationFailed;
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ EXCEPTION during registration for {Email}", user.Email);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                TempData["MessageType"] = MessageType.RegistrationFailed;
                return View(user);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // GET: /Account/AccessDenied
        // ═══════════════════════════════════════════════════════════════
       
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning("Access denied for user {User} to path {Path}",
                User?.Identity?.Name ?? "Anonymous",
                Request.Headers["Referer"].ToString());

            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Settings()
        {
            try
            {
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return RedirectToAction(nameof(Login));
                }

                var user = await _userService.GetCurrentUserByIdentityNameAsync(userEmail);
                if (user == null)
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    return RedirectToAction(nameof(Index), "Home");
                }

                var pageModel = _accountHelper.CreateSettingsPageModelFromUser(user);
                await _accountHelper.PopulateShippingPreferenceOptionsAsync(pageModel.ShippingPreferences);

                return View(pageModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading account settings for {User}", User?.Identity?.Name);
                TempData["MessageType"] = MessageType.UpdateFailed;
                return RedirectToAction(nameof(Index), "Home");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings([Bind(Prefix = "Profile")] AccountSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userOnInvalid = await LoadCurrentUserForSettingsAsync();
                var invalidModelPage = new AccountSettingsPageViewModel
                {
                    Profile = model,
                    Security = new AccountSecuritySettingsViewModel(),
                    Notifications = userOnInvalid == null
                        ? new AccountNotificationSettingsViewModel()
                        : new AccountNotificationSettingsViewModel
                        {
                            NotifyByEmail = userOnInvalid.NotifyByEmail,
                            NotifyBySms = userOnInvalid.NotifyBySms,
                            NotifyShipmentStatusUpdates = userOnInvalid.NotifyShipmentStatusUpdates,
                            NotifyMarketing = userOnInvalid.NotifyMarketing
                        },
                    ShippingPreferences = new AccountShippingPreferencesViewModel
                    {
                        DefaultCountryId = userOnInvalid?.DefaultCountryId,
                        DefaultCityId = userOnInvalid?.DefaultCityId,
                        DefaultCarrierId = userOnInvalid?.DefaultCarrierId,
                        DefaultShippingPackageId = userOnInvalid?.DefaultShippingPackageId,
                        DefaultShippingTypeId = userOnInvalid?.DefaultShippingTypeId
                    },
                    Privacy = new AccountPrivacySettingsViewModel()
                };
                await _accountHelper.PopulateShippingPreferenceOptionsAsync(invalidModelPage.ShippingPreferences);
                return View(invalidModelPage);
            }

            try
            {
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return RedirectToAction(nameof(Login));
                }

                var user = await _userService.GetUserByEmailAsync(userEmail);
                if (user == null)
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                    TempData["UpdateFailedMessage"] = ResourceTextHelper.L("SaveFailedMessage", "Unable to update account settings.");
                    return RedirectToAction(nameof(Index), "Home");
                }

                var previousPhone = user.Phone ?? string.Empty;
                var previousFirstName = user.FirstName ?? string.Empty;
                var previousLastName = user.LastName ?? string.Empty;

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Phone = model.Phone;
                var updateResult = await _userService.UpdateUserAsync(user.Id.ToString(), user);
                if (!updateResult.Success)
                {
                    //_accountHelper.AddErrorsToModelState(updateResult.Errors);
                    _accountHelper.AddErrorsToModelState(ModelState, updateResult.Errors);
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                    TempData["UpdateFailedMessage"] = ResourceTextHelper.L("SaveFailedMessage", "Unable to update account settings.");

                    var failedUpdatePage = new AccountSettingsPageViewModel
                    {
                        Profile = model,
                        Security = new AccountSecuritySettingsViewModel(),
                        Notifications = new AccountNotificationSettingsViewModel
                        {
                            NotifyByEmail = user.NotifyByEmail,
                            NotifyBySms = user.NotifyBySms,
                            NotifyShipmentStatusUpdates = user.NotifyShipmentStatusUpdates,
                            NotifyMarketing = user.NotifyMarketing
                        },
                        ShippingPreferences = new AccountShippingPreferencesViewModel
                        {
                            DefaultCountryId = user.DefaultCountryId,
                            DefaultCityId = user.DefaultCityId,
                            DefaultCarrierId = user.DefaultCarrierId,
                            DefaultShippingPackageId = user.DefaultShippingPackageId,
                            DefaultShippingTypeId = user.DefaultShippingTypeId
                        },
                        Privacy = new AccountPrivacySettingsViewModel()
                    };
                    await _accountHelper.PopulateShippingPreferenceOptionsAsync(failedUpdatePage.ShippingPreferences);
                    return View(failedUpdatePage);
                }

                var culture = (model.Language == "ar") ? "ar" : "en";

                var isPhoneChanged = !string.Equals(previousPhone, model.Phone, StringComparison.Ordinal);
                var isNameChanged =
                    !string.Equals(previousFirstName, model.FirstName, StringComparison.Ordinal) ||
                    !string.Equals(previousLastName, model.LastName, StringComparison.Ordinal);

                TempData["MessageType"] = MessageType.UpdateSuccess;
                TempData["UpdateSuccessTitle"] = ResourceTextHelper.L("UpdateSuccessTitle", "Updated Successfully.");
                if (isPhoneChanged && isNameChanged)
                {
                    TempData["UpdateSuccessMessage"] = ResourceTextHelper.L("NameAndPhoneChangedSuccessMessage", "Name and phone number changed successfully.");

                    // Send security notification to user (Phase 6 - Account Change Notifications)
                    // Respect user's notification preferences
                    if (user.NotifyByEmail)
                    {
                        try
                        {
                            await _emailSender.SendAccountNameAndPhoneChangedNotificationAsync(
                                userEmail, model.FirstName, model.LastName, model.Phone);
                            _logger.LogInformation("📧 Account change notification sent to {Email} (name and phone)", userEmail);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "❌ Failed to send account change notification to {Email}", userEmail);
                            // Don't fail the update if email sending fails
                        }
                    }
                }
                else if (isPhoneChanged)
                {
                    TempData["UpdateSuccessMessage"] = ResourceTextHelper.L("PhoneChangedSuccessMessage", "Phone number changed successfully.");

                    // Send security notification to user (Phase 6 - Account Change Notifications)
                    // Respect user's notification preferences
                    if (user.NotifyByEmail)
                    {
                        try
                        {
                            await _emailSender.SendAccountPhoneChangedNotificationAsync(userEmail, model.Phone);
                            _logger.LogInformation("📧 Account change notification sent to {Email} (phone)", userEmail);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "❌ Failed to send account change notification to {Email}", userEmail);
                            // Don't fail the update if email sending fails
                        }
                    }
                }
                else if (isNameChanged)
                {
                    TempData["UpdateSuccessMessage"] = ResourceTextHelper.L("NameChangedSuccessMessage", "Name changed successfully.");

                    // Send security notification to user (Phase 6 - Account Change Notifications)
                    // Respect user's notification preferences
                    if (user.NotifyByEmail)
                    {
                        try
                        {
                            await _emailSender.SendAccountNameChangedNotificationAsync(
                                userEmail, model.FirstName, model.LastName);
                            _logger.LogInformation("📧 Account change notification sent to {Email} (name)", userEmail);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "❌ Failed to send account change notification to {Email}", userEmail);
                            // Don't fail the update if email sending fails
                        }
                    }
                }
                else
                {
                    TempData["UpdateSuccessMessage"] = ResourceTextHelper.L("UpdateSuccessMessage", "Updated Successfully.");
                }

                return RedirectToAction("SetLanguage", "Home", new
                {
                    culture,
                    returnUrl = Url.Action(nameof(Settings), "Account")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account settings for {User}", User?.Identity?.Name);
                TempData["MessageType"] = MessageType.UpdateFailed;
                TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                TempData["UpdateFailedMessage"] = ResourceTextHelper.L("SaveFailedMessage", "Unable to update account settings.");

                var userOnException = await LoadCurrentUserForSettingsAsync();
                var exceptionPage = new AccountSettingsPageViewModel
                {
                    Profile = model,
                    Security = new AccountSecuritySettingsViewModel(),
                    Notifications = userOnException == null
                        ? new AccountNotificationSettingsViewModel()
                        : new AccountNotificationSettingsViewModel
                        {
                            NotifyByEmail = userOnException.NotifyByEmail,
                            NotifyBySms = userOnException.NotifyBySms,
                            NotifyShipmentStatusUpdates = userOnException.NotifyShipmentStatusUpdates,
                            NotifyMarketing = userOnException.NotifyMarketing
                        },
                    ShippingPreferences = new AccountShippingPreferencesViewModel
                    {
                        DefaultCountryId = userOnException?.DefaultCountryId,
                        DefaultCityId = userOnException?.DefaultCityId,
                        DefaultCarrierId = userOnException?.DefaultCarrierId,
                        DefaultShippingPackageId = userOnException?.DefaultShippingPackageId,
                        DefaultShippingTypeId = userOnException?.DefaultShippingTypeId
                    },
                    Privacy = new AccountPrivacySettingsViewModel()
                };
                await _accountHelper.PopulateShippingPreferenceOptionsAsync(exceptionPage.ShippingPreferences);
                return View(exceptionPage);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "Security")] AccountSecuritySettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["MessageType"] = MessageType.UpdateFailed;
                TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                TempData["UpdateFailedMessage"] = ResourceTextHelper.L("PasswordChangedFailedMessage", "Password change failed.");
                return RedirectToAction(nameof(Settings));
            }

            try
            {
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                    TempData["UpdateFailedMessage"] = ResourceTextHelper.L("PasswordChangedFailedMessage", "Password change failed.");
                    return RedirectToAction(nameof(Login));
                }

                // Use UserService.ChangePasswordAsync which validates current password and changes password
                // This follows the same pattern as ResetPasswordAsync but for authenticated users
                var result = await _userService.ChangePasswordAsync(userEmail, model.CurrentPassword, model.NewPassword);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("ChangePassword failed for {User}. Errors: {Errors}",
                        User?.Identity?.Name,
                        string.Join(" | ", result.Errors.Select(e => e.Description)));

                    TempData["MessageType"] = MessageType.UpdateFailed;
                    TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                    TempData["UpdateFailedMessage"] = ResourceTextHelper.L("PasswordChangedFailedMessage", "Password change failed.");
                    return RedirectToAction(nameof(Settings));
                }

                TempData["MessageType"] = MessageType.UpdateSuccess;
                TempData["UpdateSuccessTitle"] = ResourceTextHelper.L("UpdateSuccessTitle", "Updated Successfully.");
                TempData["UpdateSuccessMessage"] = ResourceTextHelper.L("PasswordChangedSuccessMessage", "Password changed successfully.");

                try
                {
                    var appUser = await _userService.FindByEmailAsync(userEmail);
                    if (appUser != null)
                    {
                        // Refresh sign-in to apply security changes
                        await _signInManager.RefreshSignInAsync(appUser);
                    }
                }
                catch (Exception refreshEx)
                {
                    _logger.LogWarning(refreshEx, "Password changed for {User} but session refresh failed.", User?.Identity?.Name);
                }

                // Send CRITICAL security notification on password change (ALWAYS send, regardless of preferences)
                // This is a mandatory security alert - password changes are critical security events
                try
                {
                    await _emailSender.SendAccountPasswordChangedNotificationAsync(userEmail);
                    _logger.LogInformation("🔐 Critical password change notification sent to {Email}", userEmail);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "❌ Failed to send password change notification to {Email}", userEmail);
                    // Don't fail the password change if email sending fails - security notification failure should not block the change
                }

                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for {User}", User?.Identity?.Name);
                TempData["MessageType"] = MessageType.UpdateFailed;
                TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                TempData["UpdateFailedMessage"] = ResourceTextHelper.L("PasswordChangedFailedMessage", "Password change failed.");
                return RedirectToAction(nameof(Settings));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotifications([Bind(Prefix = "Notifications")] AccountNotificationSettingsViewModel model)
        {
            try
            {
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return RedirectToAction(nameof(Login));
                }

                var user = await _userService.GetUserByEmailAsync(userEmail);
                if (user == null)
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                    TempData["UpdateFailedMessage"] = ResourceTextHelper.L("NotificationPreferencesChangedFailedMessage", "Notification preferences update failed.");
                    return RedirectToAction(nameof(Index), "Home");
                }

                user.NotifyByEmail = model.NotifyByEmail;
                user.NotifyBySms = model.NotifyBySms;
                user.NotifyShipmentStatusUpdates = model.NotifyShipmentStatusUpdates;
                user.NotifyMarketing = model.NotifyMarketing;

                var updateResult = await _userService.UpdateUserAsync(user.Id.ToString(), user);
                if (!updateResult.Success)
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                    TempData["UpdateFailedMessage"] = ResourceTextHelper.L("NotificationPreferencesChangedFailedMessage", "Notification preferences update failed.");
                    return RedirectToAction(nameof(Settings));
                }

                TempData["MessageType"] = MessageType.UpdateSuccess;
                TempData["UpdateSuccessTitle"] = ResourceTextHelper.L("UpdateSuccessTitle", "Updated Successfully.");
                TempData["UpdateSuccessMessage"] = ResourceTextHelper.L("NotificationPreferencesChangedSuccessMessage", "Notification preferences changed successfully.");
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification preferences for {User}", User?.Identity?.Name);
                TempData["MessageType"] = MessageType.UpdateFailed;
                TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                TempData["UpdateFailedMessage"] = ResourceTextHelper.L("NotificationPreferencesChangedFailedMessage", "Notification preferences update failed.");
                return RedirectToAction(nameof(Settings));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShippingPreferences([Bind(Prefix = "ShippingPreferences")] AccountShippingPreferencesViewModel model)
        {
            try
            {
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return RedirectToAction(nameof(Login));
                }

                var user = await _userService.GetUserByEmailAsync(userEmail);
                if (user == null)
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                    TempData["UpdateFailedMessage"] = ResourceTextHelper.L("ShippingPreferencesChangedFailedMessage", "Shipping preferences update failed.");
                    return RedirectToAction(nameof(Index), "Home");
                }

                user.DefaultCountryId = model.DefaultCountryId;
                user.DefaultCityId = model.DefaultCityId;
                user.DefaultCarrierId = model.DefaultCarrierId;
                user.DefaultShippingPackageId = model.DefaultShippingPackageId;
                user.DefaultShippingTypeId = model.DefaultShippingTypeId;

                var updateResult = await _userService.UpdateUserAsync(user.Id.ToString(), user);
                if (!updateResult.Success)
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                    TempData["UpdateFailedMessage"] = ResourceTextHelper.L("ShippingPreferencesChangedFailedMessage", "Shipping preferences update failed.");
                    return RedirectToAction(nameof(Settings));
                }

                TempData["MessageType"] = MessageType.UpdateSuccess;
                TempData["UpdateSuccessTitle"] = ResourceTextHelper.L("UpdateSuccessTitle", "Updated Successfully.");
                TempData["UpdateSuccessMessage"] = ResourceTextHelper.L("ShippingPreferencesChangedSuccessMessage", "Shipping preferences changed successfully.");
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shipping preferences for {User}", User?.Identity?.Name);
                TempData["MessageType"] = MessageType.UpdateFailed;
                TempData["UpdateFailedTitle"] = ResourceTextHelper.L("SaveFailedTitle", "Update Failed");
                TempData["UpdateFailedMessage"] = ResourceTextHelper.L("ShippingPreferencesChangedFailedMessage", "Shipping preferences update failed.");
                return RedirectToAction(nameof(Settings));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadPersonalData()
        {
            try
            {
                var result = await _userService.BuildPersonalDataExportAsync(User?.Identity?.Name);
                if (result.RequiresLogin)
                {
                    return RedirectToAction(nameof(Login));
                }

                if (!result.Success || result.Content == null)
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    return RedirectToAction(nameof(Index), "Home");
                }

                return File(result.Content, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting personal data for {User}", User?.Identity?.Name);
                TempData["MessageType"] = MessageType.UpdateFailed;
                return RedirectToAction(nameof(Settings));
            }
        }



        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAccount()
        {
            try
            {
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    return RedirectToAction(nameof(Settings));
                }

                var result = await _userService.DeactivateCurrentUserAccountAsync(userEmail);

                if (result.Success)
                {
                    // Account successfully deactivated, user is logged out
                    TempData["MessageType"] = MessageType.Deactivated;
                    TempData["Message"] = "Your account has been deactivated. You will be logged out.";
                    return RedirectToAction(nameof(Login));
                }

                TempData["MessageType"] = MessageType.UpdateFailed;
                TempData["Message"] = string.Join("; ", result.Errors ?? new[] { "Unable to deactivate account." });
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating account for {User}", User?.Identity?.Name);
                TempData["MessageType"] = MessageType.UpdateFailed;
                return RedirectToAction(nameof(Settings));
            }
        }



        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOutOtherDevices()
        {
            try
            {
                var userEmail = User?.Identity?.Name;
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    return RedirectToAction(nameof(Login));
                }

                // Use UserService to update security stamp and refresh session
                var result = await _userService.SignOutOtherDevicesAsync(userEmail);

                if (!result.Success)
                {
                    _logger.LogError("Failed to sign out other devices for {User}", userEmail);
                    TempData["MessageType"] = MessageType.UpdateFailed;
                    return RedirectToAction(nameof(Settings));
                }

                TempData["MessageType"] = MessageType.UpdateSuccess;
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing out other devices for {User}", User?.Identity?.Name);
                TempData["MessageType"] = MessageType.UpdateFailed;
                return RedirectToAction(nameof(Settings));
            }
        }



        // ═══════════════════════════════════════════════════════════════
        // 🔐 FORGOT PASSWORD
        // ═══════════════════════════════════════════════════════════════
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]  // ✅ ADDED: Rate limiting
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Call API for password reset
                var response = await _genericApiClient.PostAsync<object>(
                    "api/auth/forgot-password",
                    new { Email = model.Email });

                _logger.LogInformation("Password reset requested for {Email}", model.Email);

                return RedirectToAction(nameof(ForgotPasswordConfirmation), new { email = model.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request for {Email}", model.Email);
                return RedirectToAction(nameof(ForgotPasswordConfirmation), new { email = model.Email });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔐 RESET PASSWORD
        // ═══════════════════════════════════════════════════════════════
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Reset password page opened without required token/email");
                TempData["ErrorMessage"] = "Please use the password reset link sent to your email.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]  // ✅ ADDED: Rate limiting
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _genericApiClient.PostAsync<object>(
                    "api/auth/reset-password",
                    new
                    {
                        Email = model.Email,
                        Token = model.Token,
                        NewPassword = model.Password
                    });

                _logger.LogInformation("✅ Password reset successful for {Email}", model.Email);
                TempData["MessageType"] = MessageType.UpdateSuccess;
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Password reset failed for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Unable to reset password. The link may be invalid or expired.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ EXCEPTION during password reset for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshUserToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 Updating AccessToken in session");

                if (string.IsNullOrEmpty(request?.AccessToken))
                {
                    return BadRequest(new { success = false, message = "AccessToken is required" });
                }

                // ✅ Update claims with new token
                var existingClaims = User.Claims
                    .Where(c => c.Type != "AccessToken") // Remove Old Token
                    .ToList();

                existingClaims.Add(new Claim("AccessToken", request.AccessToken)); //Add Now Token

                var identity = new ClaimsIdentity(existingClaims, IdentityConstants.ApplicationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    IdentityConstants.ApplicationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1),
                        AllowRefresh = true
                    });

                _logger.LogInformation("✅ AccessToken updated in claims");
                return Ok(new { success = true, message = "Token updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating token");
                return StatusCode(500, new { success = false, message = "Server error" });
            }
        }

        // ✅ Model class
        public class RefreshTokenRequest
        {
            public string AccessToken { get; set; }
        }


        // ═══════════════════════════════════════════════════════════════
        // 🔧 HELPER METHODS
        // ═══════════════════════════════════════════════════════════════


        private async Task<UserDto?> LoadCurrentUserForSettingsAsync()
        {
            return await _userService.GetCurrentUserByIdentityNameAsync(User?.Identity?.Name);
        }

       

    }
}

































































































































































































































































































































































































































































































































































































