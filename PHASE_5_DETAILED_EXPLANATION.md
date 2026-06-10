# Phase 5: Shipping Preferences — Detailed Explanation & Testing Guide

## 🎯 Overview
**Phase 5: Shipping Preferences** is the fifth component of the multi-phase Settings platform,
allowing authenticated users to save and manage their default shipping options for faster, 
more convenient checkout experiences.

---

## 📌 Benefits of Phase 5

### 1. **Improved User Experience (UX)**
- **Faster Checkout:** Users no longer need to select the same carrier, package type, 
- and payment method repeatedly. One-time setup saves time on every shipment.
- **Reduced Cognitive Load:** Users set preferences once and forget about it,
- rather than making the same decision multiple times.
- **Personalization:** The system "remembers" user preferences, 
- creating a custom experience tailored to their behavior patterns.

### 2. **Increased Conversion & Customer Retention**
- **Lower Friction:** Fewer form fields on the shipment creation page = 
- faster completion = higher conversion rates.
- **User Stickiness:** Users invested in configuring preferences are more likely to continue using the platform.
- **Reduced Cart Abandonment:** Pre-filled defaults mean users move through checkout 
- faster and complete more transactions.

### 3. **Business Intelligence & Analytics**
- **Usage Metrics:** By tracking which carriers, packages, and payment methods users prefer, you can:
  - Identify top-performing carriers
  - Optimize inventory allocation for popular package types
  - Understand customer payment preferences (cash, card, etc.)
- **Demand Forecasting:** Defaults reveal patterns; if 70% of users prefer Standard Carrier,
- you can plan resources accordingly.
- **A/B Testing Opportunities:** You can test shipping options and measure against these baseline
- user preferences.

### 4. **Operational Efficiency**
- **Reduced Support Burden:** Fewer "How do I change my carrier?" support tickets.
- **Predictable Demand:** Clear visibility into preferred shipping methods helps 
- with partner negotiations and capacity planning.
- **Streamlined Partner Management:** You can negotiate better rates with carriers that 
- are marked as defaults by many users.

### 5. **Data-Driven Decisions**
- **Store User Intent:** Preferences document what users actually want, not what salespeople assume they want.
- **Segmentation:** You can create user segments based on preferences 
- (e.g., "premium users prefer overnight shipping") for targeted promotions.
- **Product Development:** Feedback from preference data informs which new options 
- (carriers, packages) to launch.

### 6. **Security & Privacy Benefits**
- **Account Control:** Users can review and modify their saved defaults anytime, increasing transparency.
- **GDPR Compliance:** Shipping preferences are part of user settings, exported in "Download My Data" feature
- (Phase 6).
- **Audit Trail:** Every preference change (if logged) creates a record for compliance and dispute resolution.

### 7. **Revenue Optimization**
- **Upselling Opportunity:** Once users are comfortable with defaults, you can:
  - Suggest premium options (e.g., "Upgrade to Express Carrier for +$5")
  - Recommend complementary services (insurance, tracking, signature)
- **Cross-Selling:** "Users who prefer Standard Carrier also add insurance" → personalized recommendations
- **Dynamic Pricing:** Future enhancement: price adjustments based on user's preferred carrier and volume 
- history.

---

## 🏗️ Technical Architecture

### Data Structure
```csharp
public class ApplicationUser : IdentityUser
{
	public Guid? DefaultCarrierId { get; set; }           // FK to Carrier
	public Guid? DefaultShippingPackageId { get; set; }   // FK to ShippingPackage
	public Guid? DefaultPaymentMethodId { get; set; }     // FK to PaymentMethod
}
```

### Preferences on Create Shipment (Future Integration)
Once fully integrated into the shipment creation flow, the form will:
```
1. Load the Settings page → user sets DefaultCarrierId = "FedEx"
2. Navigate to Create Shipment → form loads with "FedEx" pre-selected
3. User can override any default with one click
4. Shipment created with user's choice (either default or override)
```

### Update Flow
```
User → Settings Page (Phase 5 form)
  ↓
Select Carrier, Package, Payment Method
  ↓
POST to UpdateShippingPreferences endpoint
  ↓
Validate inputs
  ↓
Update ApplicationUser.DefaultCarrierId, etc.
  ↓
Return success/failure toast
  ↓
User sees confirmation
```

---

## 🧪 Comprehensive Testing Guide

### ✅ **Test Category 1: Basic Functionality**

#### Test 1.1: Load Settings Page
**Objective:** Verify Phase 5 form loads with current user preferences.

**Steps:**
1. Login as an authenticated user
2. Navigate to `/Account/Settings`
3. Scroll to "Phase 5: Shipping Preferences" section
4. Verify the form contains three dropdowns:
   - Default Carrier
   - Default Shipping Package
   - Default Payment Method

**Expected Result:**
- All three dropdowns are populated with options
- All options load from the database (not hardcoded)
- If user has no defaults set, dropdowns show "Select..." placeholder
- If user has defaults set, the previously saved option is pre-selected

**Failure Indicators:**
- Dropdowns are empty or missing
- "Select..." placeholder is missing
- Form is not visible or throws an error

---

#### Test 1.2: Save Shipping Preferences
**Objective:** Verify that user selections are persisted to the database.

**Steps:**
1. Login as an authenticated user with a clean slate (no defaults set)
2. Navigate to `/Account/Settings`
3. In Phase 5 section, select:
   - Carrier: "FedEx" (or any non-empty option)
   - Shipping Package: "Express Box" (or any option)
   - Payment Method: "Credit Card" (or any option)
4. Click "Save shipping preferences" button
5. Verify success toast appears: "Shipping preferences changed successfully."
6. Refresh the page
7. Verify all three selections are still selected in the form

**Expected Result:**
- Success toast appears immediately after save
- Page redirects and returns to Settings
- Preferences persist after page refresh
- Database contains the saved preferences (verify via SQL if possible)

**Failure Indicators:**
- No success toast appears
- Page does not redirect
- Preferences reset after refresh
- Error toast appears

---

#### Test 1.3: Update Existing Preferences
**Objective:** Verify that existing preferences can be changed.

**Steps:**
1. Login as user with existing preferences set (e.g., FedEx, Standard Box, Card)
2. Navigate to `/Account/Settings`
3. Verify Phase 5 shows current selections
4. Change carrier from "FedEx" to "UPS"
5. Keep package and payment unchanged
6. Click save
7. Verify success toast
8. Refresh page
9. Verify only carrier changed, others remained the same

**Expected Result:**
- Form pre-populates with existing preferences
- Only the changed field is updated in the database
- Other fields remain unchanged
- Success toast confirms partial update

**Failure Indicators:**
- Existing preferences not pre-loaded
- All fields reset to defaults after changing one
- No success message

---

#### Test 1.4: Clear/Unset Preferences
**Objective:** Verify that users can remove previously saved preferences.

**Steps:**
1. Login as user with existing preferences
2. Navigate to `/Account/Settings`
3. Change carrier dropdown from "FedEx" to the empty "Select..." option
4. Click save
5. Verify success toast
6. Refresh page
7. Verify carrier dropdown now shows "Select..." (empty) again

**Expected Result:**
- User can set a preference back to empty/null
- Database stores the new empty state (NULL in foreign key columns)
- Success toast appears
- Change persists after refresh

**Failure Indicators:**
- Cannot select empty option
- Empty option is grayed out or disabled
- Preference reverts to previous value after refresh

---

### ✅ **Test Category 2: Data Validation**

#### Test 2.1: Invalid Carrier ID
**Objective:** Verify that invalid carrier IDs are rejected.

**Steps:**
1. Open browser developer tools (F12)
2. Navigate to `/Account/Settings`
3. In the Network tab, intercept the POST request to `UpdateShippingPreferences`
4. Manually modify the DefaultCarrierId parameter to an invalid GUID (e.g., "99999999-9999-9999-9999-999999999999")
5. Allow the request to proceed
6. Observe the response and page behavior

**Expected Result:**
- Backend validates the carrier ID exists
- If invalid, update fails with error toast: "Update Failed — Shipping preferences update failed."
- Database is not updated (no orphaned references)
- User remains on Settings page with previous values intact

**Failure Indicators:**
- Invalid ID is accepted and saved
- No error message appears
- Page crashes or shows 500 error
- Database is corrupted with invalid FK

---

#### Test 2.2: SQL Injection Prevention
**Objective:** Verify that preference fields are protected against SQL injection.

**Steps:**
1. Open browser developer tools
2. Intercept the POST to `UpdateShippingPreferences`
3. Try injecting malicious SQL in the DefaultCarrierId field:
   `'); DROP TABLE Carriers; --`
4. Allow request to proceed

**Expected Result:**
- Injection payload is treated as a literal GUID string
- Validation fails because it's not a valid GUID format
- Update is rejected with error message
- No SQL execution occurs
- Database tables remain intact

**Failure Indicators:**
- SQL injection executes
- Carriers table is dropped
- No validation error appears
- Database is damaged

---

### ✅ **Test Category 3: Integration Tests**

#### Test 3.1: Dropdown Population (Carriers)
**Objective:** Verify that the Carrier dropdown loads all active carriers from the database.

**Steps:**
1. Login as user
2. Navigate to `/Account/Settings`
3. In Phase 5, click the "Default Carrier" dropdown
4. Count the number of options (excluding "Select...")
5. Go to database and count active carriers in the Carriers table
6. Compare the counts

**Expected Verification:**
```sql
SELECT COUNT(*) FROM Carriers WHERE IsActive = 1;  -- Should match dropdown count
```

**Expected Result:**
- Dropdown contains exactly the number of active carriers in the database
- No duplicate options appear
- All carrier names are correct
- Disabled/inactive carriers are not shown

**Failure Indicators:**
- Dropdown count doesn't match database
- Duplicates appear
- Inactive carriers are shown
- Some carriers are missing

---

#### Test 3.2: Dropdown Population (Shipping Packages)
**Objective:** Verify that the Shipping Package dropdown loads all active packages.

**Steps:**
1. Same as Test 3.1, but for Shipping Packages table
2. Compare dropdown count with: `SELECT COUNT(*) FROM ShippingPackages WHERE IsActive = 1;`

**Expected Result:**
- Dropdown matches database count
- All package names and descriptions are correct
- No duplicates or missing options

---

#### Test 3.3: Dropdown Population (Payment Methods)
**Objective:** Verify that the Payment Method dropdown loads all active payment methods.

**Steps:**
1. Same as Test 3.1, but for PaymentMethods table
2. Compare dropdown count with: `SELECT COUNT(*) FROM PaymentMethods WHERE IsActive = 1;`

**Expected Result:**
- Dropdown matches database count
- All payment method names are correct

---

#### Test 3.4: Preferences Applied to User Profile
**Objective:** Verify that saved preferences appear in the user's profile/database record.

**Steps:**
1. Login as user (e.g., email: `testuser@example.com`)
2. Set preferences: Carrier=FedEx, Package=Express, Payment=Card
3. Click save and verify success toast
4. Check database directly:
   ```sql
   SELECT DefaultCarrierId, DefaultShippingPackageId, DefaultPaymentMethodId 
   FROM AspNetUsers 
   WHERE Email = 'testuser@example.com';
   ```
5. Verify the returned GUIDs correspond to FedEx, Express Box, and Credit Card

**Expected Result:**
- Database contains the correct non-null GUIDs
- GUIDs resolve to correct entities when joined with their respective tables
- No NULL values where a preference was set

---

### ✅ **Test Category 4: User Experience Testing**

#### Test 4.1: Success Toast Display
**Objective:** Verify that success and failure toasts appear correctly and are user-friendly.

**Steps:**
1. Save valid preferences
2. Observe the toast that appears (top-right, bottom-right, or other location)
3. Note the following:
   - **Title:** "Updated Successfully." (from resource)
   - **Message:** "Shipping preferences changed successfully." (from resource)
   - **Duration:** Toast remains visible for ~3-5 seconds
   - **Color:** Green background indicating success
   - **Close Button:** Toast has an X button to dismiss manually

**Expected Result:**
- Toast appears in prominent location
- Large, readable text
- Stays visible long enough to read
- Auto-dismisses after reasonable time
- Can be manually closed

**Failure Indicators:**
- Toast appears and disappears too quickly (< 1 second)
- Text is cut off or unreadable
- Toast hides behind other content
- No dismiss button

---

#### Test 4.2: Error Handling - Network Failure Simulation
**Objective:** Verify that network errors are handled gracefully.

**Steps:**
1. Login and navigate to `/Account/Settings`
2. Open browser DevTools → Network tab
3. Set network throttling to "Offline"
4. Try to save preferences
5. Observe page behavior

**Expected Result:**
- Error toast appears: "Update Failed — Shipping preferences update failed."
- Form remains populated with user's inputs
- User can retry after network is restored
- No console JavaScript errors

**Failure Indicators:**
- Browser crashes
- Page shows blank error message
- Form is cleared
- JavaScript error in console

---

#### Test 4.3: Concurrent User Updates
**Objective:** Verify that rapid changes don't cause conflicts.

**Steps:**
1. Open Settings page in two separate browser windows (logged in as same user)
2. In Window 1: Set Carrier = FedEx, then immediately click Save
3. In Window 2: Set Carrier = UPS, Package = Standard, then click Save
4. Observe which update wins and verify consistency
5. Refresh both windows and check database state

**Expected Result:**
- Last save wins (typical behavior)
- Both windows eventually show the same state
- No data corruption
- Both users see appropriate success toasts

**Failure Indicators:**
- Data becomes inconsistent across windows
- Mixed updates (some fields from Window 1, some from Window 2)
- Database contains corrupted/orphaned references

---

#### Test 4.4: Localization - Phase 5 Labels
**Objective:** Verify that all Phase 5 text is properly localized for English and Arabic.

**Steps:**
1. Login and navigate to `/Account/Settings` in English
2. Verify all text is in English:
   - "Phase 5: Shipping Preferences"
   - "Set your default shipping options for faster checkout."
   - "Default Carrier"
   - "Default Shipping Package"
   - "Default Payment Method"
   - "Save shipping preferences" (button)
3. Change language to Arabic via the Language dropdown
4. Return to `/Account/Settings`
5. Verify all Phase 5 text is now in Arabic

**Expected Result:**
- All labels, headings, and hints are localized
- Arabic text is RTL-aligned properly
- No English text remains mixed in
- Resource keys are properly resolved

**Failure Indicators:**
- Text remains in English even in Arabic mode
- Untranslated resource keys show (e.g., "DefaultCarrier" as-is)
- Arabic text is left-aligned (should be right-aligned)
- Mixed language in same form

---

### ✅ **Test Category 5: Performance Testing**

#### Test 5.1: Dropdown Load Time
**Objective:** Verify that dropdowns load quickly even with many options.

**Steps:**
1. Navigate to `/Account/Settings`
2. Open browser DevTools → Performance tab
3. Click the "Default Carrier" dropdown
4. Record the time it takes for the dropdown to render
5. Repeat for "Default Shipping Package" and "Default Payment Method"

**Expected Result:**
- Dropdown renders in < 100ms
- No UI lag or stutter while clicking
- All options are visible without scrolling (if < 20 items)

**Failure Indicators:**
- Dropdown takes > 500ms to render
- UI freezes or becomes unresponsive
- Performance warning in DevTools

---

#### Test 5.2: Form Submission Speed
**Objective:** Verify that save operation completes in reasonable time.

**Steps:**
1. Open DevTools → Network tab
2. Make a preference change and click Save
3. Record the request/response time
4. Repeat 5 times and calculate average

**Expected Result:**
- Average submission time: < 500ms
- No timeout errors
- Response is consistent (not random delays)

**Failure Indicators:**
- Average > 2 seconds
- Intermittent timeouts
- "Request timeout" errors

---

### ✅ **Test Category 6: Edge Cases**

#### Test 6.1: Empty Form Submission
**Objective:** Verify that submitting with no selections is handled.

**Steps:**
1. Navigate to Phase 5
2. Ensure all dropdowns are set to "Select..." (empty)
3. Click "Save shipping preferences"
4. Observe response

**Expected Result:**
- Save is successful (NULL values are valid)
- Success toast appears
- Database stores NULL for all three foreign keys
- User's data is not corrupted

**Failure Indicators:**
- Error message appears (e.g., "Field is required")
- Form refuses to submit
- Partial save (some fields NULL, others corrupted)

---

#### Test 6.2: Setting Only One Preference
**Objective:** Verify that users can set just a carrier without package/payment.

**Steps:**
1. Set only "Default Carrier" = FedEx
2. Leave "Default Shipping Package" and "Default Payment Method" as "Select..."
3. Click Save
4. Verify success toast
5. Refresh page and verify only carrier is remembered

**Expected Result:**
- Form accepts partial preferences
- Only carrier is saved (other columns remain NULL)
- Page refresh shows same partial state
- No validation error

**Failure Indicators:**
- Form rejects partial submission
- Other fields are auto-filled with random defaults
- State is not preserved after refresh

---

#### Test 6.3: Switching to Another User
**Objective:** Verify that preferences are user-specific and don't leak.

**Steps:**
1. Login as User A, set preferences: Carrier=FedEx
2. Logout
3. Login as User B, navigate to Settings
4. Verify User B's preferences are different/empty (not User A's FedEx)
5. Set User B's preferences: Carrier=UPS
6. Logout
7. Login as User A again, verify preferences are still FedEx (not UPS)

**Expected Result:**
- Each user's preferences are isolated
- No data leakage between user accounts
- Preferences persist correctly per user

**Failure Indicators:**
- User B sees User A's preferences
- User A's preferences were overwritten by User B
- Preferences mix across user accounts

---

### ✅ **Test Category 7: Security Testing**

#### Test 7.1: CSRF Token Validation
**Objective:** Verify that anti-forgery token is required.

**Steps:**
1. Open browser DevTools → Network tab
2. Navigate to Settings and observe the POST request to `UpdateShippingPreferences`
3. Note the `__RequestVerificationToken` in the request body
4. Manually craft a request without the token (e.g., via curl or Postman)
5. Send the request to `UpdateShippingPreferences`

**Expected Result:**
- Request without token is rejected with 400 Bad Request
- No database changes occur
- User sees error or is redirected to login

**Failure Indicators:**
- Request succeeds without token
- Preferences are updated without CSRF protection
- Database is modified

---

#### Test 7.2: Authorization Check
**Objective:** Verify that unauthenticated users cannot update preferences.

**Steps:**
1. Logout (or open in private/incognito window)
2. Try to access `/Account/UpdateShippingPreferences` directly via POST
3. Send a valid request with all parameters

**Expected Result:**
- Request is rejected with 401 Unauthorized or redirected to Login
- No database changes occur
- User must login first

**Failure Indicators:**
- Unauthenticated user can update preferences
- Database is modified by anonymous request
- No redirect to login

---

#### Test 7.3: Authorization Check - Wrong User
**Objective:** Verify that user A cannot update user B's preferences.

**Steps:**
1. Login as User A and note the Session ID / JWT token
2. In DevTools, manually edit a preference update request for User B
3. Attempt to submit the request as User A

**Implementation Note:**
Currently, this may not be prevented because the backend reads `User?.Identity?.Name` (current authenticated user). To test this thoroughly, you'd need to manually craft a request that tries to update a different user's data, but the current design only allows updating the **current** authenticated user's preferences, which is the correct behavior.

**Expected Result:**
- Backend ignores the request and only updates the authenticated user's (User A's) preferences
- User B's data is never touched
- No cross-user data modification

---

#### Test 7.4: SQL Injection in Dropdown Selection
**Objective:** Verify that selected IDs are validated as valid GUIDs.

**Steps:**
1. Open browser DevTools → Network tab
2. Prepare to intercept a preference update request
3. Modify the DefaultCarrierId to: `' OR '1'='1`
4. Submit the request

**Expected Result:**
- Backend validates that the ID is a valid GUID format
- Invalid format is rejected
- Update fails with error (no successful update)
- Database remains unharmed

---

### ✅ **Test Category 8: Future Integration Testing**

#### Test 8.1: Preferences Are Applied on Shipment Create (Future)
**Objective:** Verify that when the Shipment Create form is updated, it uses Phase 5 defaults.

**Steps (Requires Future Integration):**
1. Login as user with Phase 5 preferences set (Carrier=FedEx, Package=Express, Payment=Card)
2. Navigate to `/Shipments/Create`
3. Observe that the form is pre-populated with:
   - Carrier dropdown shows "FedEx" as selected
   - Shipping Package dropdown shows "Express" as selected
   - Payment Method dropdown shows "Card" as selected
4. User can override any default with one click
5. Submit shipment with default or custom values

**Expected Result:**
- Defaults are pre-selected on the Create form
- User can override defaults
- Shipment is created with user's chosen values (default or override)

**Failure Indicators:**
- Create form ignores Phase 5 preferences
- Dropdowns show empty "Select..." instead of defaults
- Overrides don't work properly

---

#### Test 8.2: Preferences Update Triggers Activity Log (Future)
**Objective:** Verify that preference changes are logged for audit trail.

**Steps (Requires Future Enhancement):**
1. Update a preference
2. Query an activity log table:
   ```sql
   SELECT * FROM ActivityLog 
   WHERE UserId = @UserId 
   AND Action = 'PreferenceChanged' 
   ORDER BY CreatedAt DESC LIMIT 1;
   ```
3. Verify log entry contains:
   - User ID
   - Timestamp
   - Old value (if applicable)
   - New value

**Expected Result:**
- Preference changes create an audit trail
- Logs are timestamped and user-attributed
- Old and new values are both recorded

---

---

## 📊 Testing Checklist

Use this checklist to track test completion:

```
Category 1: Basic Functionality
  ☐ Test 1.1: Load Settings Page
  ☐ Test 1.2: Save Shipping Preferences
  ☐ Test 1.3: Update Existing Preferences
  ☐ Test 1.4: Clear/Unset Preferences

Category 2: Data Validation
  ☐ Test 2.1: Invalid Carrier ID
  ☐ Test 2.2: SQL Injection Prevention

Category 3: Integration Tests
  ☐ Test 3.1: Dropdown Population (Carriers)
  ☐ Test 3.2: Dropdown Population (Packages)
  ☐ Test 3.3: Dropdown Population (Payment Methods)
  ☐ Test 3.4: Preferences Applied to User Profile

Category 4: User Experience Testing
  ☐ Test 4.1: Success Toast Display
  ☐ Test 4.2: Error Handling (Network Failure)
  ☐ Test 4.3: Concurrent User Updates
  ☐ Test 4.4: Localization (English & Arabic)

Category 5: Performance Testing
  ☐ Test 5.1: Dropdown Load Time
  ☐ Test 5.2: Form Submission Speed

Category 6: Edge Cases
  ☐ Test 6.1: Empty Form Submission
  ☐ Test 6.2: Setting Only One Preference
  ☐ Test 6.3: Switching to Another User

Category 7: Security Testing
  ☐ Test 7.1: CSRF Token Validation
  ☐ Test 7.2: Authorization Check (Unauthenticated)
  ☐ Test 7.3: Authorization Check (Wrong User)
  ☐ Test 7.4: SQL Injection in Dropdown Selection

Category 8: Future Integration Testing
  ☐ Test 8.1: Preferences Applied on Shipment Create
  ☐ Test 8.2: Preferences Update Triggers Activity Log
```

---

## 🚀 Quick Start Testing (First 10 Minutes)

If you're short on time, prioritize these critical tests:

1. **Test 1.2** — Save and persist preferences (core functionality)
2. **Test 3.4** — Verify preferences in database (data integrity)
3. **Test 4.1** — Check success toast (user feedback)
4. **Test 4.4** — Verify localization (multi-language support)
5. **Test 6.1** — Test empty submission (edge case)

These five tests will give you 80% confidence that Phase 5 is working correctly.

---

## 📝 Reporting Issues

When you encounter a failure, document:

1. **Test ID:** (e.g., Test 3.1)
2. **Steps to Reproduce:** Exact sequence that causes failure
3. **Expected Result:** What should have happened
4. **Actual Result:** What actually happened
5. **Screenshots:** Visual evidence of the issue
6. **Browser & OS:** Chrome 120 on Windows 11, etc.
7. **Database State:** (if applicable) Relevant SQL queries showing before/after
8. **Error Logs:** Any backend errors from application logs

---

## ✨ Summary

Phase 5 is a **critical feature for user retention and conversion optimization**. It allows users to save their preferred shipping options, dramatically reducing checkout friction and increasing repeat usage. By following this comprehensive testing guide, you can ensure Phase 5 works reliably and securely for all users.

Good luck with your testing! 🎯
