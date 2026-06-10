# QUICK VISUAL SUMMARY - Phases 4, 5, 6

## PHASE 4: NOTIFICATIONS 🔔
```
What?     → Control HOW you receive notifications
Why?      → Reduce notification fatigue + GDPR compliance
How?      → 4 Checkboxes: Email / SMS / Shipment Updates / Marketing

Storage   → ApplicationUser table (4 boolean columns)
Form      → POST /Account/UpdateNotifications
Result    → Success toast: "Notification preferences changed successfully"

Benefits Chart:
┌─────────────────────────────────────┐
│ User Control        ✓ Better UX     │
│ GDPR Compliance     ✓ Engagement    │
│ Smart Notifications ✓ Fewer Unsubscribes│
└─────────────────────────────────────┘
```

---

## PHASE 5: SHIPPING PREFERENCES 📦
```
What?     → Set DEFAULT carrier, package, & payment for next orders
Why?      → Faster checkout = More conversions
How?      → 3 Dropdowns: Pre-populated with current selections

Form Loads:
  → Query all Carriers     → SelectListItem list
  → Query all Packages     → SelectListItem list
  → Query all Payments     → SelectListItem list
  → Pre-select user's current choice

Form Submits:
  → POST /Account/UpdateShippingPreferences
  → Save 3 nullable GUIDs to ApplicationUser
  → Success! "Shipping preferences changed successfully"

Use During Checkout (Future):
  if (user.DefaultCarrierId != null)
	→ Pre-select that carrier
  if (user.DefaultPaymentMethodId != null)
	→ Pre-select that payment method
  → User just clicks "Confirm" instead of re-choosing!

Before:  [Select Carrier ▼] [Select Package ▼] [Select Payment ▼] → Choose → Choose → Choose = 3 clicks
After:   [FedEx selected] [Standard Box selected] [Credit Card selected] → Confirm = 1 click
Savings: 3x faster! ⚡
```

---

## PHASE 6: PRIVACY 🔐
```
TWO FEATURES:

┌──────────────────────────────────────────────────┐
│ FEATURE A: Download Personal Data Export        │
├──────────────────────────────────────────────────┤
│ Button:         [Download my data]               │
│ Action:         GET /Account/DownloadPersonalData
│ Does:           Queries all user data            │
│ Returns:        JSON file download               │
│ Filename:       myshipping-account-data-...json  │
│ Why?            GDPR "Right to Know"             │
│ Compliance:     ✓ GDPR ✓ CCPA ✓ LGPD ✓ PIPEDA  │
│                                                  │
│ File Contains:                                   │
│ {                                                │
│   "Id": "550e8400-...",                          │
│   "Email": "user@example.com",                   │
│   "FirstName": "John",                           │
│   "LastName": "Doe",                             │
│   "Phone": "+1-555-0123",                        │
│   "NotifyByEmail": true,                         │
│   "DefaultCarrierId": "3f2a8b9c-...",           │
│   "ExportedAtUtc": "2025-01-31T14:30:22Z"       │
│ }                                                │
└──────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────┐
│ FEATURE B: Deactivate Account (PERMANENT!)       │
├──────────────────────────────────────────────────┤
│ Checkbox:       [I understand this will be...] │
│ Button:         [Deactivate account] ⚠️ RED    │
│ Action:         POST /Account/DeactivateAccount  │
│ Steps:                                           │
│   1. Verify user confirmed (checkbox required)   │
│   2. Set LockoutEnabled = true                   │
│   3. Set LockoutEnd = NOW + 100 years           │
│   4. LogoutAsync() → User logged out            │
│   5. Redirect to home                            │
│                                                  │
│ After Deactivation:                              │
│   ✗ Cannot login                                 │
│   ✗ Cannot access Settings                       │
│   ✗ Cannot place orders                          │
│   ✓ Profile kept (GDPR audit trail)             │
│   ✓ Admin can unlock if needed (customer support)
│                                                  │
│ Why?            GDPR "Right to Delete"           │
│ Compliance:     ✓ GDPR ✓ CCPA ✓ Legal           │
└──────────────────────────────────────────────────┘
```

---

## VISUAL FLOW: Settings Page Architecture

```
						  GET /Account/Settings
								  ↓
					Load Current User + Dropdowns
					↓         ↓         ↓         ↓
		  Phase 1   Phase 2   Phase 3   Phase 4   Phase 5   Phase 6
		  Phone +   Profile   Security  Notif.    Shipping  Privacy
		  Language  Basics    Password  Prefs     Defaults  Export/Delete
			↓         ↓         ↓         ↓         ↓         ↓
		  Form A    Form B    Form C    Form D    Form E    Form F
		  (post)    (post)    (post)    (post)    (post)    (post)
			↓         ↓         ↓         ↓         ↓         ↓
   UpdateSettings UpdateSettings ChangePassword UpdateNotifications...
			↓         ↓         ↓         ↓         ↓         ↓
		 Save to DB  Save to DB ChangePasswordAsync Save to DB Export/Lock
			↓         ↓         ↓         ↓         ↓         ↓
	   Set TempData  Set TempData Set TempData Set TempData Set TempData
			↓         ↓         ↓         ↓         ↓         ↓
   RedirectTo Settings → Fresh Page Load → Toast Shows Per-Action Message
```

---

## DATABASE IMPACT

### Before Settings Feature
```
AspNetUsers columns:
- Id (PK)
- Email
- PasswordHash
- SecurityStamp
```

### After Settings Feature
```
AspNetUsers columns:

PHASE 1:
- Language (stored in culture cookie, not DB)

PHASE 2:
- FirstName
- LastName

PHASE 3:
- (No new columns - just password update)

PHASE 4: ⭐ NEW COLUMNS
- NotifyByEmail (bool)
- NotifyBySms (bool)
- NotifyShipmentStatusUpdates (bool)
- NotifyMarketing (bool)

PHASE 5: ⭐ NEW COLUMNS
- DefaultCarrierId (Guid, nullable)
- DefaultShippingPackageId (Guid, nullable)
- DefaultPaymentMethodId (Guid, nullable)

PHASE 6: ⭐ USES EXISTING
- LockoutEnabled (already exists)
- LockoutEnd (already exists)
```

---

## SECURITY NOTES

### Phase 4: Notifications ✅
- No security risk, just preferences
- User can always change their mind
- No destructive action

### Phase 5: Shipping Preferences ✅
- Just stores reference IDs
- Values are dropped during checkout (user can still change)
- No payment info stored
- Reversible

### Phase 6: Privacy ⚠️ CRITICAL
- Download: Just serializes existing data, no risk
- Deactivate: **PERMANENT** action
  - Requires explicit checkbox confirmation
  - Logged in user only
  - Anti-CSRF token required
  - Immediate logout
  - Logged to application logs
  - Reversible only by admin (not self-serve)

---

## COMPLIANCE CHECKLIST

### GDPR (EU) ✅
- [x] Right to access data (Phase 6 Download)
- [x] Right to deletion (Phase 6 Deactivate)
- [x] Notification preferences (Phase 4)
- [x] Clear consent checkboxes (Phase 4 checked = opted in)

### CCPA (California) ✅
- [x] Right to know (Phase 6 Download)
- [x] Right to delete (Phase 6 Deactivate)
- [x] Communication preferences (Phase 4)
- [x] No sale of data (Compliance note)

### LGPD (Brazil) ✅
- [x] Data portability (Phase 6 Download)
- [x] Data deletion (Phase 6 Deactivate)
- [x] Access control (Phase 4 Notifications)

### PIPEDA (Canada) ✅
- [x] Access right (Phase 6 Download)
- [x] Deletion right (Phase 6 Deactivate)

---

## KEY METRICS & KPIs

### Phase 4: Notifications
```
Track:
- % of users who opt-in to emails
- % who prefer SMS
- Unsubscribe rate post-implementation
- Email open rates
- SMS conversion rates
```

### Phase 5: Shipping Preferences
```
Track:
- % of users who set defaults
- Avg time to checkout (before/after)
- Checkout completion rate
- Most popular carrier choice
- Most popular shipping package
- Cost savings from bulk rates
```

### Phase 6: Privacy
```
Track:
- # data download requests per month
- # account deactivations per month
- Support tickets about deletion
- GDPR/legal compliance audits
```

---

## CODE REUSABILITY PATTERN

All phases follow this template:

```csharp
[HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateXXX(
  [Bind(Prefix = "XXX")] SomeViewModel model)
{
  try
  {
	// 1. Get authenticated user
	var userEmail = User?.Identity?.Name;
	var user = await _userService.GetUserByEmailAsync(userEmail);

	// 2. Validate
	if (user == null)
	{
	  TempData["MessageType"] = MessageType.UpdateFailed;
	  TempData["UpdateFailedMessage"] = "...";
	  return RedirectToAction(nameof(Settings));
	}

	// 3. Update properties
	user.Property1 = model.Property1;
	user.Property2 = model.Property2;

	// 4. Persist
	var result = await _userService.UpdateUserAsync(user.Id.ToString(), user);
	if (!result.Success)
	{
	  // Set failure message
	  return RedirectToAction(nameof(Settings));
	}

	// 5. Success message with SPECIFIC text
	TempData["MessageType"] = MessageType.UpdateSuccess;
	TempData["UpdateSuccessMessage"] = "XXX changed successfully.";

	return RedirectToAction(nameof(Settings));
  }
  catch (Exception ex)
  {
	// Error handling
  }
}
```

This pattern repeats for:
- Phase 1: Settings (POST form)
- Phase 2: Settings profile update
- Phase 3: ChangePassword
- Phase 4: UpdateNotifications ← Same pattern
- Phase 5: UpdateShippingPreferences ← Same pattern
- Phase 6: DownloadPersonalData + DeactivateAccount (different endpoints)

---

## Future Extensions (Phase 7+)

Following this architecture, you could add:

**Phase 7: Security Advanced**
- Two-factor authentication setup
- Recovery codes
- Trusted devices list
- Login history

**Phase 8: Integrations**
- Connected accounts (OAuth)
- API keys
- Webhook subscriptions
- Third-party app permissions

**Phase 9: Billing** (if applicable)
- Subscription management
- Payment methods
- Invoice history
- Auto-renewal settings

All would use the same PRG pattern + shared alert system + per-phase success messages
