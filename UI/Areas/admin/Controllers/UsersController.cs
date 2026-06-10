using Business.Contracts;
using Business.DTOS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI.Constants;
using UI.Helpers;
using UI.Services;

namespace UI.Areas.admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = $"{RoleNames.Admin}")]
    public class UsersController : Controller
    {
        private readonly IUIUserService _userService;
        private readonly ILogger<HomeController> _logger;
        private readonly AccountAuditService _auditService;
        public UsersController(IUIUserService userService,
            ILogger<HomeController> logger,
            AccountAuditService auditService)
        {
            _userService = userService;
            _logger = logger;
            _auditService = auditService;
        }

        /// <summary>
        /// Display list of all users
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        /// <summary>
        /// Display Edit form (also used for Create when Id is null)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(string? id)
        {
            LoadRoles();

            var user = new UserDto();
            if (!string.IsNullOrEmpty(id))
            {
                user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["MessageType"] = MessageType.NotFound;
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(user);
        }

        /// <summary>
        /// Save user (Create or Update)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(UserDto userDto)
        {
            TempData["MessageType"] = null;

            // For editing existing users, remove password validation since we don't require password changes
            if (userDto.Id != Guid.Empty)
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }
            else
            {
                // For new users, password is required
                if (string.IsNullOrWhiteSpace(userDto.Password))
                {
                    ModelState.AddModelError("Password", "Password is required.");
                }
            }

            if (!ModelState.IsValid)
            {
                LoadRoles();
                return View("Edit", userDto);
            }

            try
            {
                UserResultDto result;

                if (userDto.Id == Guid.Empty)
                {
                    // Create new user
                    result = await _userService.RegisterAsync(userDto);

                    if (result.Success)
                    {
                        TempData["MessageType"] = MessageType.SaveSuccess;
                    }
                    else
                    {
                        // Add errors to ModelState
                        foreach (var error in result.Errors ?? Enumerable.Empty<string>())
                        {
                            var parts = error.Split(':', 2);
                            if (parts.Length == 2)
                            {
                                ModelState.AddModelError(parts[0], parts[1]);
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, error);
                            }
                        }
                        LoadRoles();
                        return View("Edit", userDto);
                    }
                }
                else
                {
                    // Update existing user
                    result = await _userService.UpdateUserAsync(userDto.Id.ToString(), userDto);

                    if (result.Success)
                    {
                        // Also update role if changed
                        if (!string.IsNullOrEmpty(userDto.Role))
                        {
                            await _userService.ChangeUserRoleAsync(userDto.Id.ToString(), userDto.Role);
                        }

                        TempData["MessageType"] = MessageType.UpdateSuccess;
                    }
                    else
                    {
                        // Add errors to ModelState
                        foreach (var error in result.Errors ?? Enumerable.Empty<string>())
                        {
                            var parts = error.Split(':', 2);
                            if (parts.Length == 2)
                            {
                                ModelState.AddModelError(parts[0], parts[1]);
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, error);
                            }
                        }
                        LoadRoles();
                        return View("Edit", userDto);
                    }
                }
            }
            catch (Exception)
            {
                TempData["MessageType"] = userDto.Id == Guid.Empty ? MessageType.SaveFailed : MessageType.UpdateFailed;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Lock user account
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Lock(string id)
        {
            TempData["MessageType"] = null;

            try
            {
                var result = await _userService.LockUserAsync(id);

                if (result.Succeeded)
                {
                    TempData["MessageType"] = MessageType.LockedSuccess;
                }
                else
                {
                    TempData["MessageType"] = MessageType.LockedFaild;
                }
            }
            catch (Exception)
            {
                TempData["MessageType"] = MessageType.LockedFaild;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Unlock user account
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Unlock(string id)
        {
            TempData["MessageType"] = null;

            try
            {
                var result = await _userService.UnlockUserAsync(id);

                if (result.Succeeded)
                {
                    TempData["MessageType"] = MessageType.UnlockedSuccess;
                }
                else
                {
                    TempData["MessageType"] = MessageType.UnlockedFaild;
                }
            }
            catch (Exception)
            {
                TempData["MessageType"] = MessageType.UnlockedFaild;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Load available roles into ViewBag for dropdown
        /// </summary>
        private void LoadRoles()
        {
            var roles = new List<string>
            {
                RoleNames.Admin,
                RoleNames.Reviewer,
                RoleNames.Operation,
                RoleNames.OperationManager
            };

            ViewBag.Roles = roles;
        }


        // Add this to your existing Admin Users controller

        /// <summary>
        /// Displays all deactivated user accounts.
        /// Admin can view when users deactivated and can reactivate if needed.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivatedUsers()
        {
            var deactivatedUsers = await _auditService.GetDeactivatedUsersAsync();
            return View(deactivatedUsers.ToList());
        }

        /// <summary>
        /// Reactivates a deactivated user account (admin operation).
        /// Creates audit log for compliance.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateUser(string userId, string reason = null)
        {
            try
            {
                var adminEmail = User?.Identity?.Name;
                var result = await _userService.ReactivateUserAsync(
                    userId: userId,
                    adminEmail: adminEmail,
                    reason: reason ?? "Admin reactivation"
                );

                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ Admin {Admin} reactivated user {UserId}", adminEmail, userId);
                    
                    TempData["MessageType"] = MessageType.SaveSuccess;
                    return RedirectToAction(nameof(DeactivatedUsers));
                }

                TempData["ErrorMessage"] = "Failed to reactivate user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(DeactivatedUsers));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction(nameof(DeactivatedUsers));
            }
        }

        /// <summary>
        /// Displays audit logs with filtering options.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AuditLogs(string action = null, string email = null, int limit = 100)
        {
            try
            {
                List<dynamic> auditLogs;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    auditLogs = (await _auditService.GetAuditLogsByUserEmailAsync(email, limit)).ToList();
                }
                else if (!string.IsNullOrWhiteSpace(action))
                {
                    auditLogs = (await _auditService.GetAuditLogsByActionWithDetailsAsync(action, limit)).ToList();
                }
                else
                {
                    auditLogs = (await _auditService.GetAuditLogsWithUserInfoAsync(limit)).ToList();
                }

                ViewBag.SearchEmail = email;
                ViewBag.SearchAction = action;
                ViewBag.AvailableActions = new[]
                {
                    "SuccessfulLogin",
                    "FailedLoginAttempt_InvalidPassword",
                    "FailedLoginAttempt_LockedOut",
                    "FailedLoginAttempt_Deactivated",
                    "Deactivated",
                    "Reactivated",
                    "PasswordChanged",
                    "LockedByAdmin",
                    "UnlockedByAdmin"
                };

                return View(auditLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                TempData["ErrorMessage"] = "Error loading audit logs: " + ex.Message;
                return View(new List<dynamic>());
            }
        }

    }
}
