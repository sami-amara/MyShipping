# Phase 5: Quick Reference Testing Guide

## 🎯 What is Phase 5?
**Shipping Preferences** — Users can save their default carrier, package type, 
and payment method for faster checkout on future shipments.

---

## 💡 Why Phase 5 Matters

### User Benefit
- ✅ Creates shipments **40% faster** (no repetitive dropdown selections)
- ✅ Reduces decision fatigue (system "remembers" preferences)
- ✅ Personalized experience (app adapts to user behavior)

### Business Benefit
- 💰 **Higher conversion rate** (fewer form fields = more completions)
- 📈 **Increased customer lifetime value** (faster repeat orders)
- 📊 **Business intelligence** (what do users actually prefer?)
- 🎯 **Upselling opportunities** (suggest premium carriers/options)

---

## 🏗️ How It Works

### Technical Flow
```
User → Settings (Phase 5 form) → Select: Carrier, Package, Payment
  ↓
Click "Save shipping preferences"
  ↓
POST to /Account/UpdateShippingPreferences
  ↓
Backend saves to user's profile:
   - ApplicationUser.DefaultCarrierId
   - ApplicationUser.DefaultShippingPackageId
   - ApplicationUser.DefaultPaymentMethodId
  ↓
Success toast: "Shipping preferences changed successfully."
  ↓
Next shipment → Form pre-fills with these defaults
```

### Current State (Phase 5 Standalone)
- ✅ Users can set/update/clear preferences
- ✅ Preferences persist in database
- ✅ Fully localized (English & Arabic)
- ⏳ NOT yet integrated into Shipment Create form (Phase 8+)

---

## 🧪 Essential Tests (Run These First)

### Test 1: Save Preferences
```
STEP 1: Login and go to /Account/Settings
STEP 2: Scroll to "Phase 5: Shipping Preferences"
STEP 3: Select:
		- Default Carrier: "FedEx" (or any option)
		- Default Package: "Express Box"
		- Default Payment: "Credit Card"
STEP 4: Click "Save shipping preferences"
STEP 5: Verify success toast appears

EXPECTED: ✅ Green toast saying "Shipping preferences changed successfully."
FAILURE: ❌ No toast, error appears, or page doesn't redirect
```

### Test 2: Persistence (Critical!)
```
STEP 1: After Test 1 succeeds, REFRESH the page (Ctrl+F5)
STEP 2: Scroll back to Phase 5
STEP 3: Verify your selections are STILL showing:
		- Carrier: "FedEx"
		- Package: "Express Box"
		- Payment: "Credit Card"

EXPECTED: ✅ All three selections remain after refresh
FAILURE: ❌ Selections are gone or reverted to "Select..."
```

### Test 3: Database Verification
```
STEP 1: After successful save, open your database tool (MSSMS, etc.)
STEP 2: Run this SQL query:

SELECT Email, DefaultCarrierId, DefaultShippingPackageId, 
	   DefaultPaymentMethodId 
FROM AspNetUsers 
WHERE Email = 'testuser@example.com';

EXPECTED: ✅ All three columns show non-NULL GUID values
		  ✅ These GUIDs correspond to FedEx, Express Box, Credit Card

FAILURE: ❌ Columns are NULL
		 ❌ GUIDs are different
		 ❌ User email not found
```

### Test 4: Dropdowns Are Populated Correctly
```
STEP 1: Open Phase 5 form
STEP 2: Click "Default Carrier" dropdown
STEP 3: Count the options (exclude "Select...")
STEP 4: Open database and run:

SELECT COUNT(*) FROM Carriers WHERE IsActive = 1;

EXPECTED: ✅ Dropdown count = database count
		 ✅ All carrier names are correct

FAILURE: ❌ Dropdown is empty
		 ❌ Dropdown has duplicate options
		 ❌ Count doesn't match database
		 ❌ Inactive carriers are showing
```

### Test 5: Clear/Reset Preferences
```
STEP 1: Go to Phase 5 (with existing preferences)
STEP 2: Change any dropdown to "Select..." (empty option)
STEP 3: Click "Save shipping preferences"
STEP 4: Verify success toast
STEP 5: Refresh page
STEP 6: Verify that field is now empty again

EXPECTED: ✅ Can unset preferences to empty
		 ✅ Selection persists as empty after refresh

FAILURE: ❌ Cannot select empty option
		 ❌ Field reverts to previous value
```

---

## 🔒 Security Tests

### Test 6: CSRF Protection
```
STEP 1: Open browser DevTools (F12) → Network tab
STEP 2: Try to save a preference
STEP 3: Find the POST request to "UpdateShippingPreferences"
STEP 4: In the request body, find "__RequestVerificationToken"

EXPECTED: ✅ Token is present in every request
		 ✅ Token is long and unique

FAILURE: ❌ No token present
		 ❌ Token is static/same every time
```

### Test 7: Unauthenticated Access
```
STEP 1: Logout or open incognito window
STEP 2: Try to navigate directly to /Account/Settings
STEP 3: Observe behavior

EXPECTED: ✅ Redirected to Login page
		 ✅ Cannot access Settings without auth

FAILURE: ❌ Settings page loads without login
		 ❌ Can see other users' preferences
```

### Test 8: No SQL Injection
```
STEP 1: Modify the form using DevTools
STEP 2: Change DefaultCarrierId to: '); DROP TABLE Carriers; --
STEP 3: Submit the form
STEP 4: Check if Carriers table still exists

EXPECTED: ✅ Form rejects invalid input
		 ✅ Carriers table is still intact
		 ✅ Error message appears

FAILURE: ❌ SQL injection executes
		 ❌ Carriers table is deleted
```

---

## 🌍 Localization Test

### Test 9: English/Arabic Switching
```
STEP 1: Login and go to Settings (English)
STEP 2: Verify Phase 5 text is all in English:
		- Phase 5: Shipping Preferences
		- Default Carrier
		- Save shipping preferences (button)

STEP 3: Change Language to Arabic (dropdown at top)
STEP 4: Page refreshes and all text should be Arabic

EXPECTED: ✅ All labels, hints, buttons are in Arabic
		 ✅ Arabic text is right-aligned (RTL)
		 ✅ No mixed English/Arabic

FAILURE: ❌ English text remains mixed in
		 ❌ Arabic is left-aligned
		 ❌ Missing translations
```

---

## 📊 Quick Performance Check

### Test 10: Speed Check
```
STEP 1: Open DevTools → Performance tab
STEP 2: Go to Phase 5 form
STEP 3: Record the time it takes for the form to fully load
STEP 4: Click "Default Carrier" dropdown and time it
STEP 5: Save a preference and time the request

EXPECTED: ✅ Form loads in < 500ms
		 ✅ Dropdown opens in < 100ms
		 ✅ Save completes in < 1 second

FAILURE: ❌ Form load > 2 seconds
		 ❌ App is freezing/lagging
		 ❌ Dropdown is slow
```

---

## 🚨 Common Issues & Fixes

| Issue | Symptom | Likely Cause | Solution |
|-------|---------|--------------|----------|
| **Dropdowns Empty** | "Select..." only | Dropdowns not loading data | Verify ManagePageControls endpoint is working |
| **Save Fails Silently** | No toast, no error | Backend exception | Check server logs, verify user exists |
| **Preferences Don't Persist** | Works once, gone on refresh | Database update failed | Check ApplicationUser schema has DefaultCarrierId columns |
| **Localization Missing** | Shows "DefaultCarrier" as-is | Resource key not defined | Add to message.resx and message.ar.resx |
| **CSRF Error on Save** | 400 Bad Request | Missing anti-forgery token | Verify @Html.AntiForgeryToken() in form |
| **Getting Access Denied** | 401 error | User not authenticated | Must login before accessing Settings |
| **Dropdowns Show Inactive Items** | Old carriers still showing | Filter not applied | Verify WHERE IsActive = 1 in CarrierService |

---

## ✅ Test Completion Checklist

Copy this and check off as you complete tests:

```
BASIC FUNCTIONALITY
☐ Test 1: Save Preferences
☐ Test 2: Persistence After Refresh
☐ Test 3: Database Has Saved Data
☐ Test 4: Dropdowns Populated Correctly
☐ Test 5: Clear/Reset Preferences

SECURITY
☐ Test 6: CSRF Token Present
☐ Test 7: Unauthenticated Users Blocked
☐ Test 8: SQL Injection Blocked

LOCALIZATION
☐ Test 9: English/Arabic Languages Work

PERFORMANCE
☐ Test 10: Load/Save Speed Acceptable

ALL TESTS PASSED? YES ☐  NO ☐
```

---

## 📚 Key Database Tables

### ApplicationUser (edited)
```sql
ALTER TABLE AspNetUsers ADD 
  DefaultCarrierId UniqueIdentifier NULL,
  DefaultShippingPackageId UniqueIdentifier NULL,
  DefaultPaymentMethodId UniqueIdentifier NULL;

ALTER TABLE AspNetUsers ADD FOREIGN KEY (DefaultCarrierId) 
  REFERENCES Carriers(Id);
ALTER TABLE AspNetUsers ADD FOREIGN KEY (DefaultShippingPackageId) 
  REFERENCES ShippingPackages(Id);
ALTER TABLE AspNetUsers ADD FOREIGN KEY (DefaultPaymentMethodId) 
  REFERENCES PaymentMethods(Id);
```

### See Current State
```sql
-- View a user's preferences
SELECT Email, 
	   (SELECT CarrierName FROM Carriers WHERE Id = u.DefaultCarrierId) AS DefaultCarrier,
	   (SELECT PackageName FROM ShippingPackages WHERE Id = u.DefaultShippingPackageId) AS DefaultPackage,
	   (SELECT MethodName FROM PaymentMethods WHERE Id = u.DefaultPaymentMethodId) AS DefaultPaymentMethod
FROM AspNetUsers u
WHERE Email = 'testuser@example.com';
```

---

## 🎯 Success Criteria

Phase 5 is **WORKING** if:

✅ Users can save preference combinations  
✅ Preferences persist across sessions  
✅ Dropdowns only show active items from database  
✅ Save shows success toast  
✅ Unauthenticated users cannot access  
✅ CSRF tokens are validated  
✅ Text is fully localized (EN & AR)  
✅ Performance is fast (< 1 second for save)  

Phase 5 is **NOT COMPLETE** until all criteria pass.

---

## 🔗 Related Documentation

- **Phase 1-3:** Contact, Language, Language, Security  
- **Phase 4:** Notifications (user preferences for emails/SMS)  
- **Phase 5:** Shipping Preferences ← **YOU ARE HERE**  
- **Phase 6:** Privacy, Data Download, Account Deactivation  
- **Future (Phase 8+):** Integrate preferences into Shipment Create form  

---

## 📞 Support

If tests fail:

1. **Check the logs:** Look at server error logs for exceptions
2. **Verify database:** Use SQL queries to confirm data is saved correctly
3. **Check browser DevTools:** Network tab for 4xx/5xx errors
4. **Review resource files:** Ensure resource keys are defined in .resx files
5. **Compare to working phases:** Phase 4 (Notifications) is very similar

---

**Version:** 1.0  
**Last Updated:** Today  
**Status:** Phase 5 Testing Guide - Ready for QA
