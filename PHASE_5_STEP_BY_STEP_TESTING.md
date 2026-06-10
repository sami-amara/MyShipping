# Phase 5: Step-by-Step Testing Execution Guide

This guide walks you through **exact, copy-paste-able steps** to test Phase 5 without guessing.

---

## 🔧 Environment Setup

### Prerequisites
- Visual Studio or code editor open
- Project built successfully: `dotnet build`
- Application running locally: `dotnet run` or F5 in Visual Studio
- SQL Server Management Studio or similar DB tool (optional but recommended)
- Browser with DevTools (Chrome, Edge, Firefox)
- Test user account created (or create during login)

### Test Data Setup
```sql
-- Verify test data exists before testing
SELECT * FROM Carriers WHERE IsActive = 1 LIMIT 5;
SELECT * FROM ShippingPackages WHERE IsActive = 1 LIMIT 5;
SELECT * FROM PaymentMethods WHERE IsActive = 1 LIMIT 5;

-- If any are empty, insert test data:
INSERT INTO Carriers (Id, CarrierName, IsActive) VALUES (NEWID(), 'FedEx', 1);
INSERT INTO Carriers (Id, CarrierName, IsActive) VALUES (NEWID(), 'UPS', 1);
INSERT INTO Carriers (Id, CarrierName, IsActive) VALUES (NEWID(), 'DHL', 1);
```

---

## 🧪 Test Suite Execution

### PHASE 5 - TEST 1: Load Settings Page (5 minutes)

**Objective:** Verify the Phase 5 form loads without errors.

**Action Steps:**

1. **Start Application**
   ```
   Open: http://localhost:<port>/Account/Login
   ```

2. **Login with Test User**
   ```
   Email: testuser@example.com
   Password: Test@123456
   [Click "Sign in"]
   ```

3. **Navigate to Settings**
   ```
   URL: http://localhost:<port>/Account/Settings
   [Or click hamburger menu → Settings]
   ```

4. **Scroll to Phase 5 Section**
   ```
   Look for heading: "Phase 5: Shipping Preferences"
   Verify description: "Set your default shipping options for faster checkout."
   ```

5. **Verify Form Structure**
   ```
   Check that form contains:
   ☐ Label: "Default Carrier" with dropdown
   ☐ Label: "Default Shipping Package" with dropdown
   ☐ Label: "Default Payment Method" with dropdown
   ☐ Button: "Save shipping preferences"
   ```

6. **Check Dropdowns Are Populated**
   ```
   Click "Default Carrier" dropdown
   Verify: Shows "Select carrier" placeholder at top
   Verify: Shows at least 2 carrier options (FedEx, UPS, DHL, etc.)
   Visual check: No "undefined" or empty values
   ```

7. **Repeat for Other Dropdowns**
   ```
   Repeat Step 6 for:
   - Default Shipping Package dropdown
   - Default Payment Method dropdown
   ```

**Expected Result:** ✅ All three dropdowns are visible, populated, and functional.

**Failure Actions:**
- If dropdown is empty → Check `ICarrier.GetAllAsync()`, verify test data exists
- If dropdown shows errors → Check browser console (F12) for JavaScript errors
- If form is missing → Check `Settings.cshtml` includes the Phase 5 section

**Screenshot to Capture:**
- Full Phase 5 form with all three dropdowns visible

---

### PHASE 5 - TEST 2: Save a Valid Preference (10 minutes)

**Objective:** Verify that user can save one or more preferences and see success feedback.

**Prerequisites:** Complete Test 1 successfully

**Action Steps:**

1. **Open Settings Page**
   ```
   Navigate to: http://localhost:<port>/Account/Settings
   Scroll to: Phase 5 section
   ```

2. **Select a Carrier**
   ```
   Click dropdown: "Default Carrier"
   Select: "FedEx" (or first available option)
   Verify: Dropdown now shows "FedEx" as selected
   ```

3. **Select a Shipping Package**
   ```
   Click dropdown: "Default Shipping Package"
   Select: First available option (e.g., "Standard Box" or "Express Box")
   Verify: Dropdown shows selected value
   ```

4. **Select a Payment Method**
   ```
   Click dropdown: "Default Payment Method"
   Select: First available option (e.g., "Credit Card" or "Cash")
   Verify: Dropdown shows selected value
   ```

5. **Submit the Form**
   ```
   Click button: "Save shipping preferences"
   Watch for: Page behavior during submission
   ```

6. **Observe Success Toast**
   ```
   Location: Top-right or bottom-right corner
   Content: Should show green toast with:
	 Title: "Updated Successfully."
	 Message: "Shipping preferences changed successfully."
   Duration: Toast should disappear after ~3-5 seconds
   ```

7. **Verify Page State**
   ```
   Check: Page did NOT do full reload (Phase 5 form still visible)
   Check: Your selections are still visible in dropdowns
   ```

**Expected Result:** ✅ Green success toast appears for 3-5 seconds, then disappears.

**Failure Actions:**
- If no toast appears → Check `site-alert-message.js` in browser console for errors
- If error toast appears (red) → Check server logs for validation errors
- If page navigates away → Normal behavior, just return to Settings to verify persistence

**Screenshot to Capture:**
- The success toast message

---

### PHASE 5 - TEST 3: Verify Persistence (5 minutes)

**Objective:** Confirm preferences are saved in database and survive page refresh.

**Prerequisites:** Complete Test 2 successfully and note your selections (e.g., FedEx, Standard Box, Card)

**Action Steps:**

1. **Hard Refresh the Page**
   ```
   Press: Ctrl+F5 (Windows) or Cmd+Shift+R (Mac)
   This clears cache and loads fresh from server
   ```

2. **Wait for Page Load**
   ```
   Wait: 2-3 seconds for page to fully load
   Verify: You are still logged in
   ```

3. **Scroll to Phase 5**
   ```
   Find: Phase 5: Shipping Preferences section
   ```

4. **Verify Selections Are Still There**
   ```
   Check Default Carrier dropdown: Should show "FedEx" (your saved selection)
   Check Default Package dropdown: Should show "Standard Box" (or your selection)
   Check Default Payment dropdown: Should show "Credit Card" (or your selection)
   ```

**Expected Result:** ✅ All three dropdowns show your saved selections (NOT "Select...").

**Failure Actions:**
- If selections are gone → Database save failed, check Test 2 server logs
- If only some selections remain → Partial save, check for database FK constraints

**SQL Verification (Optional):**
```sql
-- Run this in SSMS or your DB tool to confirm
SELECT Email, DefaultCarrierId, DefaultShippingPackageId, DefaultPaymentMethodId
FROM AspNetUsers
WHERE Email = 'testuser@example.com';

-- All three columns should have non-NULL GUID values, not just NULL
```

**Screenshot to Capture:**
- Phase 5 section after refresh showing persisted selections

---

### PHASE 5 - TEST 4: Database Verification (5 minutes)

**Objective:** Directly confirm that the database contains the correct data.

**Prerequisites:** Complete Tests 2 & 3

**Action Steps:**

1. **Open Database Tool**
   ```
   Program: SQL Server Management Studio (SSMS)
   Or: Azure Data Studio
   Or: DbVisualizer
   Connect to your local/remote database
   ```

2. **Run Query to Find Your User**
   ```sql
   SELECT Id, Email, DefaultCarrierId, DefaultShippingPackageId, DefaultPaymentMethodId
   FROM AspNetUsers
   WHERE Email = 'testuser@example.com';
   ```

3. **Verify Results**
   ```
   Expected Output:
   +------+------------------------------+----------------------------------+-----------------------------+----------------------------+
   | Id   | Email                        | DefaultCarrierId               | DefaultShippingPackageId   | DefaultPaymentMethodId     |
   +------+------------------------------+----------------------------------+-----------------------------+----------------------------+
   | ...  | testuser@example.com         | (non-NULL GUID)                | (non-NULL GUID)            | (non-NULL GUID)            |
   +------+------------------------------+----------------------------------+-----------------------------+----------------------------+

   Check: All three columns have values (not NULL)
   ```

4. **Verify the GUIDs Reference Correct Entities**
   ```sql
   -- Get the carrier name
   SELECT CarrierName FROM Carriers WHERE Id = '<DefaultCarrierId value from above>';

   -- Get the package name
   SELECT PackageName FROM ShippingPackages WHERE Id = '<DefaultShippingPackageId value from above>';

   -- Get the payment method name
   SELECT MethodName FROM PaymentMethods WHERE Id = '<DefaultPaymentMethodId value from above>';
   ```

5. **Verify Results Match Your Selections**
   ```
   Compare:
   - Carrier name should be "FedEx" (or whatever you selected)
   - Package name should be "Standard Box" (or whatever you selected)
   - Payment method should be "Credit Card" (or whatever you selected)
   ```

**Expected Result:** ✅ Database contains correct non-NULL GUIDs that reference the correct entities.

**Failure Actions:**
- If columns are NULL → Save operation didn't execute, check Test 2 again
- If GUIDs don't resolve → Foreign key constraint issue, check schema
- If GUIDs are wrong values → Selection mapping issue in controller

**Screenshot to Capture:**
- SQL query results showing the data

---

### PHASE 5 - TEST 5: Dropdown Population (5 minutes)

**Objective:** Verify dropdowns only show **active** entities from database.

**Prerequisites:** Test 1 passed, have access to database

**Action Steps:**

1. **Database: Count Active Carriers**
   ```sql
   SELECT COUNT(*) AS ActiveCarrierCount 
   FROM Carriers 
   WHERE IsActive = 1;

   -- Save this count: ___________
   ```

2. **UI: Count Dropdown Options**
   ```
   Open: http://localhost:<port>/Account/Settings
   Scroll to: Phase 5 section
   Click: "Default Carrier" dropdown
   Count: Number of options (excluding "Select carrier" placeholder)
   Save this count: ___________
   ```

3. **Verify Counts Match**
   ```
   Database Count: 5 (example)
   Dropdown Count: 5 (expected)

   Are they equal? ☐ Yes ☐ No
   ```

4. **Repeat for Other Dropdowns**
   ```sql
   SELECT COUNT(*) FROM ShippingPackages WHERE IsActive = 1;
   SELECT COUNT(*) FROM PaymentMethods WHERE IsActive = 1;
   ```
   Then count options in UI for each and verify match.

5. **Check for Inactive Items**
   ```sql
   -- Verify no inactive items are showing
   SELECT * FROM Carriers WHERE IsActive = 0;
   -- These should NOT appear in the dropdown
   ```

**Expected Result:** ✅ Dropdown count exactly matches database count of active items.

**Failure Actions:**
- If dropdown has extra items → Inactive items are being shown, check `BuildCarrierOptionsAsync()` filter
- If dropdown has missing items → Some active items aren't loading, check service logic
- If counts differ → Data mismatch between database and UI layer

**Screenshot to Capture:**
- Dropdown open showing list of options

---

### PHASE 5 - TEST 6: Partial Save (10 minutes)

**Objective:** Verify users can set only some preferences (not all three).

**Prerequisites:** Tests 1-5 passed

**Action Steps:**

1. **Clear Previous Preferences (Optional)**
   ```
   In Phase 5, set each dropdown back to "Select..." option
   Click "Save shipping preferences"
   Wait for success toast
   This gives you a clean slate for this test
   ```

2. **Set Only the Carrier**
   ```
   Click: "Default Carrier" dropdown
   Select: "FedEx"
   Leave: "Default Shipping Package" as "Select..."
   Leave: "Default Payment Method" as "Select..."
   Click: "Save shipping preferences"
   Observe: Success toast appears
   ```

3. **Verify Only Carrier Was Saved**
   ```sql
   SELECT DefaultCarrierId, DefaultShippingPackageId, DefaultPaymentMethodId
   FROM AspNetUsers
   WHERE Email = 'testuser@example.com';

   Expected:
   - DefaultCarrierId: (non-NULL GUID for FedEx)
   - DefaultShippingPackageId: NULL
   - DefaultPaymentMethodId: NULL
   ```

4. **Refresh UI and Verify**
   ```
   Refresh: http://localhost:<port>/Account/Settings
   Verify:
   - Carrier dropdown shows "FedEx" (saved)
   - Package dropdown shows "Select..." (empty)
   - Payment dropdown shows "Select..." (empty)
   ```

5. **Now Add a Second Preference**
   ```
   Click: "Default Shipping Package" dropdown
   Select: "Express Box"
   Leave: Payment as "Select..."
   Click: "Save shipping preferences"
   Wait for success toast
   ```

6. **Verify Database**
   ```sql
   SELECT DefaultCarrierId, DefaultShippingPackageId, DefaultPaymentMethodId
   FROM AspNetUsers
   WHERE Email = 'testuser@example.com';

   Expected:
   - DefaultCarrierId: (FedEx GUID - unchanged)
   - DefaultShippingPackageId: (Express Box GUID - new)
   - DefaultPaymentMethodId: NULL (still empty)
   ```

**Expected Result:** ✅ Users can save partial preferences; each save only updates changed fields.

**Failure Actions:**
- If all fields reset to NULL → Save operation overwrites everything, needs fix in controller
- If empty selections overwrite previous values → Logic needs adjustment

---

### PHASE 5 - TEST 7: Clear Preferences (10 minutes)

**Objective:** Verify users can unset preferences back to empty.

**Prerequisites:** Tests 1-6 passed

**Action Steps:**

1. **Current State Check**
   ```
   Verify user has preferences set:
   - Carrier: FedEx
   - Package: Express Box
   - Payment: (might be empty, that's ok)
   ```

2. **Clear One Preference**
   ```
   Open: http://localhost:<port>/Account/Settings
   Click: "Default Carrier" dropdown
   Select: "Select carrier" (the empty option at top)
   Leave other fields unchanged
   Click: "Save shipping preferences"
   Observe: Success toast
   ```

3. **Verify Cleared in Database**
   ```sql
   SELECT DefaultCarrierId, DefaultShippingPackageId, DefaultPaymentMethodId
   FROM AspNetUsers
   WHERE Email = 'testuser@example.com';

   Expected:
   - DefaultCarrierId: NULL (cleared)
   - DefaultShippingPackageId: (still has value)
   - DefaultPaymentMethodId: NULL (or has value if set)
   ```

4. **Refresh UI and Verify**
   ```
   Refresh: http://localhost:<port>/Account/Settings
   Check: Carrier dropdown shows "Select carrier" (empty, not FedEx)
   Check: Package dropdown still shows "Express Box" (unchanged)
   ```

5. **Clear All Preferences**
   ```
   In Phase 5:
   - Set Carrier to "Select carrier"
   - Set Package to "Select package"
   - Set Payment to "Select payment"
   Click: "Save shipping preferences"
   Wait for success toast
   ```

6. **Verify All Cleared in Database**
   ```sql
   SELECT DefaultCarrierId, DefaultShippingPackageId, DefaultPaymentMethodId
   FROM AspNetUsers
   WHERE Email = 'testuser@example.com';

   Expected: All three columns are NULL
   ```

**Expected Result:** ✅ Users can unset any/all preferences; cleared values become NULL.

**Failure Actions:**
- If empty option is grayed out → Dropdown doesn't allow selection, needs fix in view
- If cleared values revert → Database update not working for empty values
- If partial clear fails → Some fields can't be independently cleared

---

### PHASE 5 - TEST 8: Localization (10 minutes)

**Objective:** Verify Phase 5 text is properly translated to Arabic and English.

**Prerequisites:** Tests 1-5 passed, user has multi-language capability

**Action Steps:**

1. **Verify English Version**
   ```
   Ensure browser language setting is English or navigate with culture=en
   URL: http://localhost:<port>/Account/Settings?culture=en
   Scroll to: Phase 5 section

   Verify these texts are in English:
   ☐ "Phase 5: Shipping Preferences" (heading)
   ☐ "Set your default shipping options for faster checkout." (description)
   ☐ "Default Carrier" (label)
   ☐ "Default Shipping Package" (label)
   ☐ "Default Payment Method" (label)
   ☐ "Save shipping preferences" (button text)
   ☐ "Select carrier" (dropdown placeholder)
   ☐ "Select shipping package" (dropdown placeholder)
   ☐ "Select payment method" (dropdown placeholder)
   ```

2. **Switch to Arabic**
   ```
   Option A: Use dropdown at top of page:
	 - Look for language selector
	 - Click on "العربية" (Arabic)
	 - Page refreshes

   Option B: Use URL directly:
	 - Open: http://localhost:<port>/Account/Settings?culture=ar
   ```

3. **Wait for Page Load**
   ```
   Wait: 2-3 seconds for translation
   Verify: Page direction changed to RTL (right-to-left)
   ```

4. **Verify Arabic Version**
   ```
   Scroll to: Phase 5 section

   Verify these texts are now in Arabic:
   ☐ Phase 5 heading is Arabic
   ☐ Description is Arabic
   ☐ All labels are Arabic
   ☐ Button text is Arabic
   ☐ Placeholder text in dropdowns is Arabic

   NO English text should remain visible
   ```

5. **Check Right-to-Left (RTL) Alignment**
   ```
   Observe the layout:
   - Labels should be on the right side
   - Dropdowns should have RTL styling
   - Arabic text should flow from right to left
   - NOT left-to-right like English
   ```

6. **Verify Dropdown Options Are Translated**
   ```
   Click: "Default Carrier" dropdown
   Verify: Options are still showing carrier names (these are NOT translatable)
   If showing as "Select carrier" → this should be Arabic now
   ```

7. **Switch Back to English**
   ```
   Use language selector dropdown again
   Select: "English"
   Wait for page refresh
   Verify: All texts are back in English
   ```

**Expected Result:** ✅ All Phase 5 text is properly localized (English → Arabic, Arabic → English), with correct RTL alignment for Arabic.

**Failure Actions:**
- If text is still English in Arabic mode → Resource keys not defined in message.ar.resx
- If Arabic is left-aligned → CSS RTL rules not applied
- If dropdowns show untranslated keys (e.g., "DefaultCarrier") → Check resource file format

**Resource File Verification:**
```
File: ../Shipping1/AppResource/message.ar.resx

Should contain keys like:
✓ SettingsPhase5Title
✓ ShippingPreferencesDescription
✓ DefaultCarrier
✓ DefaultShippingPackage
✓ DefaultPaymentMethod
✓ SaveShippingPreferences
✓ SelectCarrier
✓ SelectShippingPackage
✓ SelectPaymentMethod

Each with Arabic translation in the <value> element
```

**Screenshot to Capture:**
- Phase 5 section in Arabic showing RTL alignment

---

### PHASE 5 - TEST 9: Error Handling (10 minutes)

**Objective:** Verify that errors are handled gracefully with user-friendly messages.

**Prerequisites:** Tests 1-8 passed

**Action Steps:**

1. **Test Invalid Carrier ID (Database Level)**
   ```
   Open browser: F12 (DevTools)
   Navigate to: Network tab
   Open Settings page, scroll to Phase 5

   Prepare to intercept POST request:
   - Select a valid carrier
   - In Network tab, right-click → "Break on request"
   - Filter to "UpdateShippingPreferences"
   - Click "Save shipping preferences" button
   ```

2. **Modify Request**
   ```
   In Network tab, find POST to UpdateShippingPreferences
   In request body, find: "DefaultCarrierId"
   Change value to: "99999999-9999-9999-9999-999999999999"
	 (a GUID that doesn't exist in database)
   Right-click request → "Edit and resend"
   ```

3. **Observe Response**
   ```
   Check response:
   - Status: Should be validation error (400) or save error
   - Body: Might show error details

   Check UI:
   - Error toast should appear (red background)
   - Message: "Update Failed — Shipping preferences update failed."
   ```

4. **Verify Database Unchanged**
   ```sql
   SELECT DefaultCarrierId FROM AspNetUsers WHERE Email = 'testuser@example.com';
   -- Should still show the previous valid carrier or NULL, NOT the invalid ID
   ```

5. **Test Network Error Simulation**
   ```
   In DevTools → Network tab
   Find throttling dropdown (usually shows "No throttling")
   Set to: "Offline"

   Try to save a preference
   Observe:
   - Request fails
   - Error toast appears
   - Form data is preserved
   ```

6. **Restore Network**
   ```
   In DevTools, set throttling back to "No throttling"
   Try to save again
   Should succeed this time
   ```

**Expected Result:** ✅ Errors are handled gracefully with user-friendly toast messages, no data corruption.

**Failure Actions:**
- If invalid ID is saved to database → Validation missing
- If no error message appears → Error handling missing in controller
- If form data is lost → Poor UX, form should retain values

---

### PHASE 5 - TEST 10: CSRF Protection (10 minutes)

**Objective:** Verify anti-forgery tokens are present and validated.

**Prerequisites:** Tests 1-5 passed

**Action Steps:**

1. **Inspect Form Token**
   ```
   Open: http://localhost:<port>/Account/Settings
   Right-click page → "Inspect" or press F12
   In Elements/Inspector tab, find the Shippin Preferences form

   Look for:
   <input name="__RequestVerificationToken" type="hidden" value="..." />

   Verify:
   ☐ Token field exists
   ☐ Token has a long value (not empty, not static)
   ☐ Token value changes on each page load (test by refreshing)
   ```

2. **Capture Network Traffic**
   ```
   Open DevTools → Network tab
   Set filter to: XHR/Fetch
   Make a preference change and click Save
   Find POST request to "UpdateShippingPreferences"
   ```

3. **View Request Body**
   ```
   Right-click request → "Show request body"
   Or expand the request details

   Look for:
   __RequestVerificationToken=<long token value>

   Verify: Token is present in POST body
   ```

4. **Verify Token Validation**
   ```
   Store the token value: ___________________

   Now try to craft a manual request without this token:

   Using curl or Postman:
   POST http://localhost:<port>/Account/UpdateShippingPreferences
   Headers:
	 Content-Type: application/x-www-form-urlencoded
   Body:
	 Notifications__NS__Default=FedEx (or similar)
	 [But NO __RequestVerificationToken field]

   Expected result: 400 Bad Request or 403 Forbidden
   ```

5. **Test Token Mismatch**
   ```
   OR try with a mismatched token:
   POST with __RequestVerificationToken=fake_invalid_token

   Expected: Request rejected
   ```

**Expected Result:** ✅ CSRF token is present, unique, and validated before save operations.

**Failure Actions:**
- If no token in form → Add @Html.AntiForgeryToken() to form
- If token is static → Token generation issue
- If POST succeeds without token → CSRF protection missing, security issue!

---

### PHASE 5 - TEST 11: Security - Unauthenticated Access (5 minutes)

**Objective:** Verify that unauthenticated users cannot save preferences.

**Prerequisites:** No special setup required

**Action Steps:**

1. **Open Incognito/Private Window**
   ```
   Browser: Chrome/Edge/Firefox
   New incognito/private window
   Navigate to: http://localhost:<port>/Account/Settings
   ```

2. **Observe Login Redirect**
   ```
   Expected: Should NOT see Settings page
   Expected: Should be redirected to Login page
   Check URL: Should be .../Account/Login (not Settings)
   ```

3. **Attempt Direct POST (Programmatic Test)**
   ```
   Using curl or Postman (not in browser):

   POST http://localhost:<port>/Account/UpdateShippingPreferences
   Headers:
	 Content-Type: application/x-www-form-urlencoded
   Body:
	 ShippingPreferences__NS__DefaultCarrierId=<some guid>

   Expected Response: 401 Unauthorized or redirect to login
   ```

4. **Verify No Preferences Were Saved**
   ```sql
   -- Check if a new user was created or if any data was saved
   -- There should be NO unauthenticated user record
   ```

**Expected Result:** ✅ Unauthenticated users are blocked from accessing Settings and cannot save preferences.

**Failure Actions:**
- If Settings page loads without login → [Authorize] attribute missing
- If POST succeeds without auth → Authorization check missing, security issue!
- If new user is created → No auth validation, data integrity issue!

---

### PHASE 5 - TEST 12: Performance - Load Time (5 minutes)

**Objective:** Ensure Phase 5 form loads quickly.

**Prerequisites:** Tests 1-5 passed

**Action Steps:**

1. **Open DevTools Performance**
   ```
   Browser DevTools F12
   Go to: Performance tab
   (Warning: DevTools will slow things down, so take results with grain of salt)
   ```

2. **Measure Form Load**
   ```
   Click: Record button in Performance tab
   Navigate: http://localhost:<port>/Account/Settings
   Scroll to: Phase 5 section
   Stop recording after form is visible

   Analyze:
   - "Largest Contentful Paint" (LCP): < 2 seconds
   - Time to interactive: < 3 seconds
   - No long tasks > 50ms
   ```

3. **Measure Dropdown Open Time**
   ```
   Start recording
   Click: Carrier dropdown
   Stop recording after options are visible

   Expected: < 100ms to display
   ```

4. **Measure Save Request Time**
   ```
   Go to: Network tab (Filter: XHR)
   Click: Save preferences
   Look at POST request to UpdateShippingPreferences

   Time: Should be < 500ms (excluding network latency)
   ```

**Expected Result:** ✅ Form loads, dropdowns open, and saves complete in reasonable time.

**Failure Actions:**
- If load > 3 seconds → Investigate slow queries or large data sets
- If dropdowns are slow → Too many options or pagination needed
- If save > 1 second → Slow database or business logic

---

## 📋 Final Checklist

After completing all 12 tests, mark your results:

```
TEST RESULTS

☐ Test 1: Load Settings Page ..................... PASS / FAIL
☐ Test 2: Save Valid Preference ................. PASS / FAIL
☐ Test 3: Persistence After Refresh ............ PASS / FAIL
☐ Test 4: Database Verification ................ PASS / FAIL
☐ Test 5: Dropdown Population .................. PASS / FAIL
☐ Test 6: Partial Save ......................... PASS / FAIL
☐ Test 7: Clear Preferences .................... PASS / FAIL
☐ Test 8: Localization (EN & AR) ............... PASS / FAIL
☐ Test 9: Error Handling ....................... PASS / FAIL
☐ Test 10: CSRF Protection ..................... PASS / FAIL
☐ Test 11: Unauthenticated Access ............. PASS / FAIL
☐ Test 12: Performance ......................... PASS / FAIL

OVERALL RESULT: ________ (ALL PASS = READY FOR PRODUCTION)
```

---

## 🚨 If Anything Fails

1. **Note exactly which test failed**
2. **Document the step that failed**
3. **Capture screenshot of the error**
4. **Check server logs:** `dotnet run` output or Event Viewer
5. **Check browser console:** F12 → Console tab for JS errors
6. **Check browser network:** F12 → Network tab for HTTP errors
7. **Run the relevant SQL query** to verify database state
8. **Create a bug ticket** with: test #, steps, expected, actual, logs, screenshots

---

**Total Estimated Time:** 60-90 minutes for all tests  
**Minimum Time:** 25 minutes for critical tests only (1, 2, 3, 4, 5)  
**Recommended:** Run all tests for production readiness

Good luck! 🎯
