# Phase 4, 5, 6 - Code Deep Dive with Examples

---

# PHASE 4: NOTIFICATIONS - Code Examples

## View (Settings.cshtml) - Lines 105-146

```html
<div class="card mb-4">
	<div class="card-body p-4">
		<h5 class="mb-3">@ResourceTextHelper.L("SettingsPhase4Title", "Phase 4: Notifications")</h5>
		<p class="text-muted mb-4">
			@ResourceTextHelper.L("NotificationSettingsDescription", 
			  "Choose how you want to receive account and shipment updates.")
		</p>

		<!-- SEPARATE FORM for notifications only -->
		<form asp-action="UpdateNotifications" 
			  asp-controller="Account" 
			  method="post">
			@Html.AntiForgeryToken()

			<!-- Checkbox 1: Email Notifications -->
			<div class="form-check mb-2">
				<input asp-for="Notifications.NotifyByEmail" class="form-check-input" />
				<label asp-for="Notifications.NotifyByEmail" class="form-check-label">
					@ResourceTextHelper.L("NotifyByEmail", "Email notifications")
				</label>
			</div>

			<!-- Checkbox 2: SMS Notifications -->
			<div class="form-check mb-2">
				<input asp-for="Notifications.NotifyBySms" class="form-check-input" />
				<label asp-for="Notifications.NotifyBySms" class="form-check-label">
					@ResourceTextHelper.L("NotifyBySms", "SMS notifications")
				</label>
			</div>

			<!-- Checkbox 3: Shipment Status Updates -->
			<div class="form-check mb-2">
				<input asp-for="Notifications.NotifyShipmentStatusUpdates" class="form-check-input" />
				<label asp-for="Notifications.NotifyShipmentStatusUpdates" class="form-check-label">
					@ResourceTextHelper.L("NotifyShipmentStatusUpdates", "Shipment status updates")
				</label>
			</div>

			<!-- Checkbox 4: Marketing Updates -->
			<div class="form-check mb-4">
				<input asp-for="Notifications.NotifyMarketing" class="form-check-input" />
				<label asp-for="Notifications.NotifyMarketing" class="form-check-label">
					@ResourceTextHelper.L("NotifyMarketing", "Marketing updates")
				</label>
			</div>

			<!-- Submit Button -->
			<button type="submit" class="wshipping-button">
				@ResourceTextHelper.L("SaveNotificationPreferences", "Save notification preferences")
			</button>
		</form>
	</div>
</div>
```

## ViewModel - AccountNotificationSettingsViewModel.cs

```csharp
namespace UI.Models
{
	/// <summary>
	/// View model for Phase 4: Notification Preferences
	/// Each boolean flag represents a communication channel the user can opt into/out of
	/// </summary>
	public class AccountNotificationSettingsViewModel
	{
		/// <summary>
		/// User wants to receive email notifications
		/// </summary>
		public bool NotifyByEmail { get; set; }

		/// <summary>
		/// User wants to receive SMS/text message notifications
		/// </summary>
		public bool NotifyBySms { get; set; }

		/// <summary>
		/// User wants to receive updates when their shipments change status
		/// (e.g., "Order picked up", "In transit", "Delivered")
		/// </summary>
		public bool NotifyShipmentStatusUpdates { get; set; }

		/// <summary>
		/// User opts into marketing/promotional emails
		/// IMPORTANT: Must have explicit user consent for compliance (GDPR)
		/// </summary>
		public bool NotifyMarketing { get; set; }
	}
}
```

## Controller - AccountController.UpdateNotifications()

```csharp
/// <summary>
/// POST /Account/UpdateNotifications
/// Allows authenticated user to update their notification preferences
/// Each checkbox state is persisted to the database
/// </summary>
[HttpPost]
[Authorize]  // Only logged-in users
[ValidateAntiForgeryToken]  // CSRF protection
public async Task<IActionResult> UpdateNotifications(
	[Bind(Prefix = "Notifications")] // Binds to Notifications section of view model
	AccountNotificationSettingsViewModel model)
{
	try
	{
		// STEP 1: Get currently authenticated user's email from claims
		var userEmail = User?.Identity?.Name;
		if (string.IsNullOrWhiteSpace(userEmail))
		{
			// Should not happen if [Authorize] is working, but defensive coding
			return RedirectToAction(nameof(Login));
		}

		// STEP 2: Query database for current user record
		var user = await _userService.GetUserByEmailAsync(userEmail);
		if (user == null)
		{
			// User deleted between requests? Shouldn't happen...
			TempData["MessageType"] = MessageType.UpdateFailed;
			TempData["UpdateFailedTitle"] = ResourceTextHelper.L(
				"SaveFailedTitle", "Update Failed");
			TempData["UpdateFailedMessage"] = ResourceTextHelper.L(
				"NotificationPreferencesChangedFailedMessage", 
				"Notification preferences update failed.");
			return RedirectToAction(nameof(Index), "Home");
		}

		// STEP 3: Update user object with form values
		// Note: These are separate from user.Email, user.FirstName, etc.
		// So changing notifications won't accidentally overwrite other fields
		user.NotifyByEmail = model.NotifyByEmail;
		user.NotifyBySms = model.NotifyBySms;
		user.NotifyShipmentStatusUpdates = model.NotifyShipmentStatusUpdates;
		user.NotifyMarketing = model.NotifyMarketing;

		// STEP 4: Save changes to database
		var updateResult = await _userService.UpdateUserAsync(
			user.Id.ToString(), 
			user  // Full UserDto with updated preferences
		);

		// STEP 5: Check if save succeeded
		if (!updateResult.Success)
		{
			// Database error, validation error, etc.
			_logger.LogWarning(
				"Notification preferences update failed for {User}. Errors: {Errors}",
				User?.Identity?.Name,
				string.Join(" | ", updateResult.Errors));

			// Set failure alert
			TempData["MessageType"] = MessageType.UpdateFailed;
			TempData["UpdateFailedTitle"] = ResourceTextHelper.L(
				"SaveFailedTitle", "Update Failed");
			TempData["UpdateFailedMessage"] = ResourceTextHelper.L(
				"NotificationPreferencesChangedFailedMessage", 
				"Notification preferences update failed.");

			return RedirectToAction(nameof(Settings));
		}

		// STEP 6: Success! Set TempData for toast notification
		TempData["MessageType"] = MessageType.UpdateSuccess;
		TempData["UpdateSuccessTitle"] = ResourceTextHelper.L(
			"UpdateSuccessTitle", "Updated Successfully.");
		TempData["UpdateSuccessMessage"] = ResourceTextHelper.L(
			"NotificationPreferencesChangedSuccessMessage", 
			"Notification preferences changed successfully.");

		// Use shared alert system (see _AlertMessage.cshtml)
		// Toast will show: "Updated Successfully. / Notification preferences changed successfully."

		// STEP 7: Redirect to GET /Account/Settings
		// This triggers page reload, displaying the alert toast
		return RedirectToAction(nameof(Settings));
	}
	catch (Exception ex)
	{
		// Log unexpected errors for debugging
		_logger.LogError(
			ex, 
			"Error updating notification preferences for {User}", 
			User?.Identity?.Name);

		// Show generic failure message
		TempData["MessageType"] = MessageType.UpdateFailed;
		TempData["UpdateFailedTitle"] = ResourceTextHelper.L(
			"SaveFailedTitle", "Update Failed");
		TempData["UpdateFailedMessage"] = ResourceTextHelper.L(
			"NotificationPreferencesChangedFailedMessage", 
			"Notification preferences update failed.");

		return RedirectToAction(nameof(Settings));
	}
}
```

## Database Schema Impact

```sql
-- Migration: AddUserNotificationPreferences.cs
ALTER TABLE AspNetUsers ADD COLUMN NotifyByEmail BIT NOT NULL DEFAULT 1;
ALTER TABLE AspNetUsers ADD COLUMN NotifyBySms BIT NOT NULL DEFAULT 0;
ALTER TABLE AspNetUsers ADD COLUMN NotifyShipmentStatusUpdates BIT NOT NULL DEFAULT 1;
ALTER TABLE AspNetUsers ADD COLUMN NotifyMarketing BIT NOT NULL DEFAULT 0;

-- Entity mapping in ApplicationUser.cs
public class ApplicationUser : IdentityUser
{
	public bool NotifyByEmail { get; set; } = true;
	public bool NotifyBySms { get; set; } = false;
	public bool NotifyShipmentStatusUpdates { get; set; } = true;
	public bool NotifyMarketing { get; set; } = false;
}
```

## Real-World Usage During Shipment

```csharp
// In ShipmentService.cs - When shipment status changes
public async Task UpdateShipmentStatusAsync(string shipmentId, ShipmentStatus newStatus)
{
	var shipment = await _shipmentRepository.GetAsync(shipmentId);
	var user = await _userManager.FindByIdAsync(shipment.UserId);

	// Update shipment status
	shipment.Status = newStatus;
	await _shipmentRepository.UpdateAsync(shipment);

	// ONLY send notification if user enabled it
	if (user.NotifyShipmentStatusUpdates)  // ← Check Phase 4 preference!
	{
		// Send email OR SMS based on preferences
		if (user.NotifyByEmail)
			await _emailService.SendShipmentStatusUpdateAsync(user.Email, shipment);

		if (user.NotifyBySms)
			await _smsService.SendShipmentStatusUpdateAsync(user.Phone, shipment);
	}

	// If user said NO, we skip all notifications → Respects user choice
}
```

---

# PHASE 5: SHIPPING PREFERENCES - Code Examples

## View (Settings.cshtml) - Lines 147-181

```html
<div class="card mb-4">
	<div class="card-body p-4">
		<h5 class="mb-3">
			@ResourceTextHelper.L("SettingsPhase5Title", "Phase 5: Shipping Preferences")
		</h5>
		<p class="text-muted mb-4">
			@ResourceTextHelper.L("ShippingPreferencesDescription", 
			  "Set your default shipping options for faster checkout.")
		</p>

		<!-- SEPARATE FORM for shipping preferences -->
		<form asp-action="UpdateShippingPreferences" 
			  asp-controller="Account" 
			  method="post">
			@Html.AntiForgeryToken()

			<!-- Dropdown 1: Select Default Carrier -->
			<div class="mb-3">
				<label asp-for="ShippingPreferences.DefaultCarrierId" class="form-label">
					@ResourceTextHelper.L("DefaultCarrier", "Default Carrier")
				</label>
				<select asp-for="ShippingPreferences.DefaultCarrierId" 
						asp-items="Model.ShippingPreferences.Carriers"  <!-- Pre-populated by controller -->
						class="form-select">
					<option value="">
						@ResourceTextHelper.L("SelectCarrier", "Select carrier")
					</option>
				</select>
			</div>

			<!-- Dropdown 2: Select Default Shipping Package -->
			<div class="mb-3">
				<label asp-for="ShippingPreferences.DefaultShippingPackageId" class="form-label">
					@ResourceTextHelper.L("DefaultShippingPackage", "Default Shipping Package")
				</label>
				<select asp-for="ShippingPreferences.DefaultShippingPackageId" 
						asp-items="Model.ShippingPreferences.ShippingPackages"  <!-- Pre-populated -->
						class="form-select">
					<option value="">
						@ResourceTextHelper.L("SelectShippingPackage", "Select shipping package")
					</option>
				</select>
			</div>

			<!-- Dropdown 3: Select Default Payment Method -->
			<div class="mb-4">
				<label asp-for="ShippingPreferences.DefaultPaymentMethodId" class="form-label">
					@ResourceTextHelper.L("DefaultPaymentMethod", "Default Payment Method")
				</label>
				<select asp-for="ShippingPreferences.DefaultPaymentMethodId" 
						asp-items="Model.ShippingPreferences.PaymentMethods"  <!-- Pre-populated -->
						class="form-select">
					<option value="">
						@ResourceTextHelper.L("SelectPaymentMethod", "Select payment method")
					</option>
				</select>
			</div>

			<!-- Submit Button -->
			<button type="submit" class="wshipping-button">
				@ResourceTextHelper.L("SaveShippingPreferences", "Save shipping preferences")
			</button>
		</form>
	</div>
</div>
```

## ViewModel - AccountShippingPreferencesViewModel.cs

```csharp
namespace UI.Models
{
	/// <summary>
	/// View model for Phase 5: Shipping Preferences
	/// Allows users to set default shipping options that pre-populate during checkout
	/// </summary>
	public class AccountShippingPreferencesViewModel
	{
		/// <summary>
		/// Foreign key to preferred carrier (Carriers table)
		/// Nullable: User hasn't selected a default yet
		/// </summary>
		public Guid? DefaultCarrierId { get; set; }

		/// <summary>
		/// Foreign key to preferred shipping package/size (ShippingPackages table)
		/// Nullable: User hasn't selected a default yet
		/// </summary>
		public Guid? DefaultShippingPackageId { get; set; }

		/// <summary>
		/// Foreign key to preferred payment method (PaymentMethods table)
		/// Nullable: User hasn't selected a default yet
		/// </summary>
		public Guid? DefaultPaymentMethodId { get; set; }

		/// <summary>
		/// Pre-populated dropdown options for UI rendering
		/// Built by controller using ICarrier, IShippingPackage, IPaymentMethods services
		/// </summary>
		public List<SelectListItem> Carriers { get; set; } = new();
		public List<SelectListItem> ShippingPackages { get; set; } = new();
		public List<SelectListItem> PaymentMethods { get; set; } = new();
	}
}
```

## Controller - Build Dropdowns Helper Methods

```csharp
/// <summary>
/// Builds SelectListItem dropdown for carriers
/// Each item shows carrier name, value is carrier ID GUID
/// Marks the user's current preference as selected
/// </summary>
private async Task<List<SelectListItem>> BuildCarrierOptionsAsync(Guid? selectedId)
{
	try
	{
		// Query all carriers from database/API
		var carriers = await _carrier.GetAllCarriersAsync();

		// Convert to SelectListItem
		var items = carriers.Select(c => new SelectListItem
		{
			Value = c.Id.ToString(),           // Unique ID
			Text = c.Name,                     // Display name (e.g., "FedEx", "UPS")
			Selected = (c.Id == selectedId)    // Pre-select user's current choice
		}).ToList();

		// Add blank option at top
		items.Insert(0, new SelectListItem 
		{ 
			Value = "", 
			Text = ResourceTextHelper.L("SelectCarrier", "Select carrier"),
			Selected = (selectedId == null)
		});

		return items;
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error building carrier options");
		return new List<SelectListItem> 
		{ 
			new() { Value = "", Text = "Error loading carriers" } 
		};
	}
}

/// <summary>
/// Similar pattern for shipping packages
/// </summary>
private async Task<List<SelectListItem>> BuildShippingPackageOptionsAsync(Guid? selectedId)
{
	try
	{
		var packages = await _shippingPackage.GetAllPackagesAsync();

		var items = packages.Select(p => new SelectListItem
		{
			Value = p.Id.ToString(),
			Text = $"{p.Name} - {p.Dimensions}", // "Small Padded - 10x10x5"
			Selected = (p.Id == selectedId)
		}).ToList();

		items.Insert(0, new SelectListItem 
		{ 
			Value = "", 
			Text = ResourceTextHelper.L("SelectShippingPackage", "Select shipping package"),
			Selected = (selectedId == null)
		});

		return items;
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error building package options");
		return new List<SelectListItem>();
	}
}

/// <summary>
/// Similar pattern for payment methods
/// </summary>
private async Task<List<SelectListItem>> BuildPaymentMethodOptionsAsync(Guid? selectedId)
{
	try
	{
		var methods = await _paymentMethods.GetAllPaymentMethodsAsync();

		var items = methods.Select(m => new SelectListItem
		{
			Value = m.Id.ToString(),
			Text = m.DisplayName,  // "Visa ending in 4242"
			Selected = (m.Id == selectedId)
		}).ToList();

		items.Insert(0, new SelectListItem 
		{ 
			Value = "", 
			Text = ResourceTextHelper.L("SelectPaymentMethod", "Select payment method"),
			Selected = (selectedId == null)
		});

		return items;
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error building payment method options");
		return new List<SelectListItem>();
	}
}
```

## Controller - UpdateShippingPreferences()

```csharp
/// <summary>
/// POST /Account/UpdateShippingPreferences
/// Saves user's default carrier, package, and payment method
/// These become pre-selections during checkout
/// </summary>
[HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateShippingPreferences(
	[Bind(Prefix = "ShippingPreferences")] 
	AccountShippingPreferencesViewModel model)
{
	try
	{
		// STEP 1: Get authenticated user
		var userEmail = User?.Identity?.Name;
		if (string.IsNullOrWhiteSpace(userEmail))
		{
			return RedirectToAction(nameof(Login));
		}

		// STEP 2: Fetch user from DB
		var user = await _userService.GetUserByEmailAsync(userEmail);
		if (user == null)
		{
			TempData["MessageType"] = MessageType.UpdateFailed;
			TempData["UpdateFailedTitle"] = ResourceTextHelper.L(
				"SaveFailedTitle", "Update Failed");
			TempData["UpdateFailedMessage"] = ResourceTextHelper.L(
				"ShippingPreferencesChangedFailedMessage", 
				"Shipping preferences update failed.");
			return RedirectToAction(nameof(Index), "Home");
		}

		// STEP 3: Update user's preference IDs
		// Note: model contains nullable GUIDs (can be null if user selected "-- Select --")
		user.DefaultCarrierId = model.DefaultCarrierId;
		user.DefaultShippingPackageId = model.DefaultShippingPackageId;
		user.DefaultPaymentMethodId = model.DefaultPaymentMethodId;

		// STEP 4: Save to database
		var updateResult = await _userService.UpdateUserAsync(
			user.Id.ToString(), 
			user
		);

		if (!updateResult.Success)
		{
			_logger.LogWarning(
				"Shipping preferences update failed for {User}. Errors: {Errors}",
				User?.Identity?.Name,
				string.Join(" | ", updateResult.Errors));

			TempData["MessageType"] = MessageType.UpdateFailed;
			TempData["UpdateFailedTitle"] = ResourceTextHelper.L(
				"SaveFailedTitle", "Update Failed");
			TempData["UpdateFailedMessage"] = ResourceTextHelper.L(
				"ShippingPreferencesChangedFailedMessage", 
				"Shipping preferences update failed.");

			return RedirectToAction(nameof(Settings));
		}

		// STEP 5: Success alert
		TempData["MessageType"] = MessageType.UpdateSuccess;
		TempData["UpdateSuccessTitle"] = ResourceTextHelper.L(
			"UpdateSuccessTitle", "Updated Successfully.");
		TempData["UpdateSuccessMessage"] = ResourceTextHelper.L(
			"ShippingPreferencesChangedSuccessMessage", 
			"Shipping preferences changed successfully.");

		return RedirectToAction(nameof(Settings));
	}
	catch (Exception ex)
	{
		_logger.LogError(
			ex, 
			"Error updating shipping preferences for {User}", 
			User?.Identity?.Name);

		TempData["MessageType"] = MessageType.UpdateFailed;
		TempData["UpdateFailedTitle"] = ResourceTextHelper.L(
			"SaveFailedTitle", "Update Failed");
		TempData["UpdateFailedMessage"] = ResourceTextHelper.L(
			"ShippingPreferencesChangedFailedMessage", 
			"Shipping preferences update failed.");

		return RedirectToAction(nameof(Settings));
	}
}
```

## Integration: Using Preferences During Checkout

```csharp
// In ShipmentController - When creating new shipment
public async Task<IActionResult> Create()
{
	// Get authenticated user
	var user = await _userService.GetUserByEmailAsync(User?.Identity?.Name);

	var model = new CreateShipmentViewModel();

	// PRE-FILL with user's preferences (Phase 5)
	if (user.DefaultCarrierId.HasValue)
	{
		model.SelectedCarrierId = user.DefaultCarrierId;  // ← Pre-selected
		var carrier = await _carrier.GetCarrierAsync(user.DefaultCarrierId.Value);
		model.CarrierName = carrier.Name;  // Display: "Your usual carrier: FedEx"
	}

	if (user.DefaultShippingPackageId.HasValue)
	{
		model.SelectedPackageId = user.DefaultShippingPackageId;  // ← Pre-selected
	}

	if (user.DefaultPaymentMethodId.HasValue)
	{
		model.SelectedPaymentMethodId = user.DefaultPaymentMethodId;  // ← Pre-selected
	}

	// Build dropdowns with user's preference marked selected
	model.AvailableCarriers = await BuildCarrierDropdownAsync(user.DefaultCarrierId);
	model.AvailablePackages = await BuildPackageDropdownAsync(user.DefaultShippingPackageId);
	model.AvailablePaymentMethods = await BuildPaymentDropdownAsync(user.DefaultPaymentMethodId);

	return View(model);  // Render with defaults pre-selected
}
```

---

# PHASE 6: PRIVACY - Code Examples

## View (Settings.cshtml) - Lines 182-219

```html
<div class="card">
	<div class="card-body p-4">
		<h5 class="mb-3">
			@ResourceTextHelper.L("SettingsPhase6Title", "Phase 6: Privacy")
		</h5>
		<p class="text-muted mb-4">
			@ResourceTextHelper.L("PrivacySettingsDescription", 
			  "Manage your personal data and account lifecycle.")
		</p>

		<!-- FEATURE A: Download Personal Data -->
		<div class="d-flex flex-wrap gap-2 mb-4">
			<form asp-action="DownloadPersonalData" 
				  asp-controller="Account" 
				  method="post">
				@Html.AntiForgeryToken()
				<button type="submit" class="btn btn-outline-primary">
					@ResourceTextHelper.L("DownloadMyData", "Download my data")
				</button>
			</form>
		</div>

		<!-- FEATURE B: Deactivate Account -->
		<form asp-action="DeactivateAccount" 
			  asp-controller="Account" 
			  method="post">
			@Html.AntiForgeryToken()

			<!-- Confirmation checkbox - MUST be checked to enable button -->
			<div class="form-check mb-3">
				<input asp-for="Privacy.ConfirmDeactivate" 
					   class="form-check-input" />
				<label asp-for="Privacy.ConfirmDeactivate" class="form-check-label">
					@ResourceTextHelper.L("ConfirmDeactivateAccount", 
					  "I understand this account will be deactivated")
				</label>
			</div>

			<!-- Danger button - only enabled if checkbox is checked (via JavaScript) -->
			<button type="submit" 
					class="btn btn-outline-danger"
					id="deactivateBtn"
					disabled>
				@ResourceTextHelper.L("DeactivateAccount", "Deactivate account")
			</button>
		</form>

		<!-- Client-side: Enable button only if checkbox is checked -->
		<script>
			const confirmCheckbox = document.querySelector('input[for="Privacy.ConfirmDeactivate"]');
			const deactivateBtn = document.getElementById('deactivateBtn');

			confirmCheckbox?.addEventListener('change', function() {
				deactivateBtn.disabled = !this.checked;
			});
		</script>
	</div>
</div>
```

## ViewModel - AccountPrivacySettingsViewModel.cs

```csharp
namespace UI.Models
{
	/// <summary>
	/// View model for Phase 6: Privacy Actions
	/// IMPORTANT: Currently only used for deactivation confirmation
	/// Future: Could expand for data retention policies, etc.
	/// </summary>
	public class AccountPrivacySettingsViewModel
	{
		/// <summary>
		/// User must explicitly check this box before account deactivation
		/// Confirms they understand the irreversible nature of the action
		/// GDPR requires explicit informed consent for deletion
		/// </summary>
		public bool ConfirmDeactivate { get; set; }
	}
}
```

## Controller - DownloadPersonalData()

```csharp
/// <summary>
/// POST /Account/DownloadPersonalData
/// Exports all user's personal data as JSON file download
/// Complies with GDPR "Right to Know" provision
/// </summary>
[HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DownloadPersonalData()
{
	try
	{
		// STEP 1: Get authenticated user
		var userEmail = User?.Identity?.Name;
		if (string.IsNullOrWhiteSpace(userEmail))
		{
			_logger.LogWarning("DownloadPersonalData called without authenticated user");
			return RedirectToAction(nameof(Login));
		}

		// STEP 2: Fetch all user data from database
		var user = await _userService.GetUserByEmailAsync(userEmail);
		if (user == null)
		{
			_logger.LogWarning("DownloadPersonalData: User not found - {Email}", userEmail);
			TempData["MessageType"] = MessageType.UpdateFailed;
			return RedirectToAction(nameof(Index), "Home");
		}

		// STEP 3: Build export payload with ALL user information
		// This includes:
		// - Identity info (email, names)
		// - Preferences (Phase 4)
		// - Defaults (Phase 5)
		// - Metadata (export timestamp)
		var payload = new
		{
			UserId = user.Id,
			user.Email,
			user.FirstName,
			user.LastName,
			user.Phone,

			// Phase 4 Notification Preferences
			user.NotifyByEmail,
			user.NotifyBySms,
			user.NotifyShipmentStatusUpdates,
			user.NotifyMarketing,

			// Phase 5 Shipping Preferences (IDs)
			user.DefaultCarrierId,
			user.DefaultShippingPackageId,
			user.DefaultPaymentMethodId,

			// Audit trail: When was this exported?
			ExportedAtUtc = DateTime.UtcNow,

			// Note: Password hash, security stamp, etc. are NEVER exported
			// This is just user-facing data
		};

		// STEP 4: Serialize to pretty-printed JSON
		var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
		{
			WriteIndented = true  // Pretty-print for human readability
		});

		// STEP 5: Convert JSON string to binary
		var bytes = Encoding.UTF8.GetBytes(json);

		// STEP 6: Generate filename with timestamp
		var fileName = $"myshipping-account-data-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
		// Example: "myshipping-account-data-20250131143022.json"

		// STEP 7: Log the export for audit purposes
		_logger.LogInformation(
			"User {Email} (ID: {UserId}) downloaded personal data export",
			user.Email,
			user.Id);

		// STEP 8: Return file download
		// Browser will download JSON file instead of displaying in page
		return File(
			fileContents: bytes,
			contentType: "application/json",
			fileDownloadName: fileName
		);
	}
	catch (Exception ex)
	{
		_logger.LogError(
			ex, 
			"Error exporting personal data for {User}", 
			User?.Identity?.Name);

		TempData["MessageType"] = MessageType.UpdateFailed;
		return RedirectToAction(nameof(Settings));
	}
}
```

## Controller - DeactivateAccount()

```csharp
/// <summary>
/// POST /Account/DeactivateAccount
/// PERMANENTLY locks user account (sets lockout to 100 years in future)
/// Does NOT delete data, but prevents login
/// Complies with GDPR "Right to Delete" (with retention for legal/audit)
/// </summary>
[HttpPost]
[Authorize]  // Only authenticated users can deactivate their own account
[ValidateAntiForgeryToken]  // CSRF protection - critical for destructive action
public async Task<IActionResult> DeactivateAccount(
	[Bind(Prefix = "Privacy")] 
	AccountPrivacySettingsViewModel model)
{
	// STEP 1: Validate explicit confirmation
	// User MUST have checked "I understand..." checkbox
	if (!model.ConfirmDeactivate)
	{
		_logger.LogWarning(
			"DeactivateAccount called without confirmation checkbox for {User}",
			User?.Identity?.Name);

		TempData["MessageType"] = MessageType.Warning;
		// Don't show detailed error - prevent enumeration attacks
		return RedirectToAction(nameof(Settings));
	}

	try
	{
		// STEP 2: Get current user from Identity system
		var appUser = await _userManager.GetUserAsync(User);
		if (appUser == null)
		{
			// User was deleted between requests? Shouldn't happen
			_logger.LogError("DeactivateAccount: User not found in UserManager");
			TempData["MessageType"] = MessageType.UpdateFailed;
			return RedirectToAction(nameof(Login));
		}

		// STEP 3: Enable lockout on user account
		// Must be done before setting LockoutEnd date
		var lockResult = await _userManager.SetLockoutEnabledAsync(appUser, true);
		if (!lockResult.Succeeded)
		{
			_logger.LogError(
				"DeactivateAccount: Failed to enable lockout for {User}. Errors: {Errors}",
				appUser.Email,
				string.Join(" | ", lockResult.Errors.Select(e => e.Description)));

			TempData["MessageType"] = MessageType.UpdateFailed;
			return RedirectToAction(nameof(Settings));
		}

		// STEP 4: Set lockout end date to 100 years in future
		// This is the "soft delete" approach:
		// - Account is inaccessible
		// - Data still exists (GDPR compliance, legal hold)
		// - Admin can manually unlock if needed (customer service case)
		var lockEndDate = DateTimeOffset.UtcNow.AddYears(100);

		var lockEndResult = await _userManager.SetLockoutEndDateAsync(
			appUser, 
			lockEndDate
		);

		if (!lockEndResult.Succeeded)
		{
			_logger.LogError(
				"DeactivateAccount: Failed to set lockout end date for {User}. Errors: {Errors}",
				appUser.Email,
				string.Join(" | ", lockEndResult.Errors.Select(e => e.Description)));

			TempData["MessageType"] = MessageType.UpdateFailed;
			return RedirectToAction(nameof(Settings));
		}

		// STEP 5: Log the deactivation for audit purposes
		_logger.LogInformation(
			"Account deactivated for {Email} (ID: {UserId}) at {UtcNow}",
			appUser.Email,
			appUser.Id,
			DateTime.UtcNow);

		// STEP 6: Log user out immediately
		// They can't use the account anymore, so sign them out
		await _userService.LogoutAsync();

		// STEP 7: Set success message
		TempData["MessageType"] = MessageType.UpdateSuccess;
		// Note: Don't detail the success message - keep it brief
		// Message: "Account deactivated successfully"

		// STEP 8: Redirect to home page
		// User sees public page, can't access Settings anymore
		return RedirectToAction(nameof(Index), "Home");
	}
	catch (Exception ex)
	{
		// Log unexpected errors
		_logger.LogError(
			ex, 
			"Error deactivating account for {User}", 
			User?.Identity?.Name);

		TempData["MessageType"] = MessageType.UpdateFailed;
		return RedirectToAction(nameof(Settings));
	}
}
```

## Login Security: Checking Lockout Status

```csharp
// In UserService.LoginAsync()
public async Task<UserResultDto> LoginAsync(LoginDto loginDto)
{
	// ... existing validation ...

	var user = await _userManager.FindByEmailAsync(loginDto.Email);

	// CHECK 1: Is account locked out?
	if (user.LockoutEnabled && user.LockoutEnd > DateTime.UtcNow)
	{
		// Account is deactivated → Deny login
		return new UserResultDto
		{
			Success = false,
			IsLockedOut = true,  // Signal to show "Account deactivated" message
			Errors = new[] { "This account has been deactivated." }
		};
	}

	// ... continue with login ...
}
```

---

# Summary: All Three Phases Working Together

```
User Settings Page
	↓
┌─────────────────────────────────────────────┐
│ Phase 4: Notifications                      │
│ 4 checkboxes → 4 boolean DB columns        │
│ Used by: ShipmentService.UpdateStatusAsync  │
├─────────────────────────────────────────────┤
│ Phase 5: Shipping Preferences               │
│ 3 dropdowns → 3 nullable GUID FK columns   │
│ Used by: CreateShipmentController.Create() │
├─────────────────────────────────────────────┤
│ Phase 6: Privacy                            │
│ - Download button → File export             │
│ - Deactivate button → Account lockout       │
│ - Both require explicit confirmation        │
└─────────────────────────────────────────────┘
	↓
All use same pattern:
  1. Get authenticated user
  2. Load/validate data
  3. Update database
  4. Set TempData success/failure
  5. Redirect to GET /Settings
  6. Toast appears with specific message
```
