# PayPal Integration Progress Summary

**Date Created:** Current Session  
**Project:** MyShipping - Payment System Integration  
**Workspace:** E:\MyShipping\  
**Framework:** .NET 9, ASP.NET Core WebApi + Razor Pages  
**Latest Update:** Option A Implementation Complete ✅

---

## 🎯 Current Status

✅ **BACKEND COMPLETE** - PayPal async/redirect flow fully implemented  
⚠️ **FRONTEND PENDING** - Need to add PayPal redirect logic and callback pages

See **[OPTION_A_IMPLEMENTATION_COMPLETE.md](OPTION_A_IMPLEMENTATION_COMPLETE.md)** for complete implementation details and next steps.

---

## 📊 Session Progress Overview

### ✅ Completed Work

1. **Payment System Foundation (Previous Sessions)**
   - ✅ Stripe gateway fully functional
   - ✅ Shipment rollback on payment failure
   - ✅ DB-driven payment methods (no hardcoded values)
   - ✅ Payment method seeder service
   - ✅ Active-only payment methods with validation
   - ✅ Persistent idempotency for duplicate prevention
   - ✅ Webhook event persistence and deduplication
   - ✅ Transaction reconciliation and audit tracking
   - ✅ Professional XML documentation across payment stack
   - ✅ Comprehensive test suite (9/9 tests passing in `WebApi.Tests`)

2. **PayPal Gateway Implementation**
   - ✅ PayPal REST API integration for orders, captures, refunds
   - ✅ Real webhook signature verification using PayPal verification API
   - ✅ OAuth 2.0 access token management with auto-refresh
   - ✅ Sandbox/Live environment configuration support

3. **Local Webhook Testing Setup**
   - ✅ ngrok installed and authenticated on `E:\`
   - ✅ Public forwarding URL obtained: `https://shortcut-crushing-conjoined.ngrok-free.dev`
   - ✅ PayPal webhook endpoint: `https://shortcut-crushing-conjoined.ngrok-free.dev/api/PaymentWebhooks/paypal`

4. **PayPal Implementation Bug Fixes**
   - ✅ Fixed `System.UriFormatException` - BaseUrl null handling with Sandbox/Live fallback
   - ✅ Fixed `invalid_client` error - Corrected Sandbox vs Live URL logic inversion
   - ✅ Fixed empty token response - Added `JsonPropertyName` for snake_case mapping
   - ✅ Fixed capture logic - Removed incorrect `PaymentMethodToken` requirement
   - ✅ Fixed `ORDER_NOT_APPROVED` - Implemented proper redirect/approval flow (Option A)
   - ✅ Added HttpClient registrations for PayPal and Stripe in RegisterServicesHelper
   - ✅ Fixed all compile errors from Option A refactor

5. **Option A: Async PayPal Flow Implementation** ✅
   - ✅ Split payment into order creation + capture-after-approval
   - ✅ `PayPalPaymentGateway.ProcessPayment()` now returns approval URL
   - ✅ Added `PayPalPaymentGateway.CaptureOrder(orderId)` method
   - ✅ Created `PayPalCallbackController` for return/cancel handling
   - ✅ Extended `IPaymentGatewayFactory` with `GetGatewayByNameAsync()`
   - ✅ Extended `IPaymentTransactionService` with `ReconcileTransactionFromCallback()`
   - ✅ Added `AdditionalInfo` to `PaymentResult`, `PaymentTransactionDto`, and `TbPaymentTransaction`
   - ✅ Updated `ShipmentCommandService` to detect approval-required scenarios
   - ✅ Build successful - all compile errors resolved


   - ✅ **CRITICAL FIX:** URL logic was inverted (Sandbox → Live URL, Live → Sandbox URL)
	 - Issue: Sandbox credentials sent to Live PayPal API, causing `invalid_client` rejection
	 - Solution: Changed condition from `Environment == "Sandbox"` to `Environment == "Live"`
   - ✅ **JSON MAPPING FIX:** PayPal token response deserialization failing
	 - Issue: PayPal returns `access_token` (snake_case), but model had `AccessToken` (PascalCase)
	 - Solution: Added `[JsonPropertyName("access_token")]` attributes to map property names correctly
	 - Also enhanced error message to show actual PayPal response body for debugging
   - ✅ **PAYMENT CAPTURE FIX:** Orders created but not captured (no money moved)
	 - Issue: Capture logic required `PaymentMethodToken` to be non-empty, but it's null for server-side payments
	 - Impact: 3 out of 4 test payments created orders but didn't capture them → not visible in PayPal transactions
	 - Solution: Removed unnecessary `PaymentMethodToken` check; now captures whenever `CaptureImmediately = true`
	 - Added better error handling when capture fails or is pending

### 🔄 Current Status: PAYMENT FLOW REDESIGNED ⚠️

**Latest Issue:** `ORDER_NOT_APPROVED` error when trying to capture PayPal order  
**Root Cause:** PayPal requires user approval before capture for standard e-commerce payments  
**Solution:** Redesigned flow to match PayPal's redirect/approval model

**What Changed:**
- `ProcessPayment` now creates order and returns approval URL (doesn't capture)
- Added new `CaptureOrder` method to capture after user approves
- Payment returns `RequiresAction` status instead of `Completed`
- Frontend needs to redirect user to PayPal for approval
- Callback endpoint needed to capture after user returns

**Critical:** This is a **BREAKING CHANGE** - shipment creation flow needs updates!

**See:** `PAYPAL_PAYMENT_FLOW_CHANGES.md` for complete implementation guide

**Next Step:** Decide on async vs sync payment flow and implement callback handling

---

## 🔧 Technical Configuration

### Current PayPal Settings (`WebApi/appsettings.json`)

```json
"PaymentGateways": {
  "PayPal": {
	"Enabled": true,
	"ClientId": "ASJj66E3uTsql8Zc5jD3Sqpf80S6n3O5hBQ0fFoojvzKend7D7ftN9cnxGTgSDdFKeOk6MkjFbh51hb7",
	"ClientSecret": "EGQKYqwU8TDd1txU_XVYU0PgSHlK4RWDh5Aqo2oXmC5y4Q3lul9u86i5t6zfjNbbb0-3Hv5FmydMXrAC",
	"WebhookId": "YOUR_PAYPAL_WEBHOOK_ID",
	"Environment": "Sandbox"
	// BaseUrl removed - auto-defaults to sandbox URL
  }
}
```

**Notes:**
- `BaseUrl` is now optional and auto-computed from `Environment`
- Sandbox URL: `https://api-m.sandbox.paypal.com`
- Live URL: `https://api-m.paypal.com`
- `WebhookId` placeholder needs to be replaced after creating webhook in PayPal dashboard

### ngrok Configuration

**Installation Path:** `E:\ngrok.exe`  
**Forwarding URL:** `https://shortcut-crushing-conjoined.ngrok-free.dev` → `http://localhost:5228`  
**Start Command:** `.\ngrok http 5228` (run from `E:\` in PowerShell)

**Important:** ngrok URL changes each time you restart it (unless you have a paid account with reserved domains)

---

## 🐛 Debugging Context

### Current Exception Flow

1. User creates shipment through UI
2. `ShipmentCommandService.Create()` calls payment service
3. `PaymentTransactionService.ProcessPayment()` gets PayPal gateway
4. `PayPalPaymentGateway` constructor initializes successfully (✅ fixed)
5. `ProcessPayment()` calls `EnsureAccessToken()`
6. **OAuth token request to PayPal fails** ❌ ← Current failure point

### Likely Root Causes (To Investigate)

1. **Invalid Credentials**
   - ClientId/ClientSecret may not match
   - Credentials might be for Live environment but using Sandbox URL
   - App might not be activated in PayPal Sandbox Dashboard

2. **Environment Mismatch**
   - Using Live credentials with Sandbox environment setting
   - Or vice versa

3. **PayPal Account Status**
   - Sandbox app not activated
   - Credentials expired or revoked

4. **Network Issues**
   - SSL/TLS connection problems
   - Firewall blocking PayPal API

### How to Diagnose

**Next run will show detailed error like:**
```
Failed to obtain PayPal access token. Status: 401, Error: {"error":"invalid_client","error_description":"Client Authentication failed"}
```

Common PayPal OAuth errors:
- `invalid_client` (401) → Wrong ClientId/ClientSecret
- `invalid_scope` (400) → Scope issue (shouldn't happen with client_credentials)
- `unauthorized_client` (401) → App not activated or disabled

---

## 📁 Key Files Modified (This Session)

### 1. `Business/Services/PaymentGateways/PayPalPaymentGateway.cs`

**Constructor Fix (Lines 53-66):**
```csharp
_httpClient = httpClientFactory.CreateClient("PayPal");

// Set base URL based on environment (Sandbox for testing, Live for production)
var baseUrl = _options.BaseUrl;

// If BaseUrl is not explicitly configured, determine from environment
if (string.IsNullOrWhiteSpace(baseUrl))
{
	baseUrl = string.Equals(_options.Environment, "Live", StringComparison.OrdinalIgnoreCase)
		? "https://api-m.paypal.com" 
		: "https://api-m.sandbox.paypal.com";
}

_httpClient.BaseAddress = new Uri(baseUrl);
```

**Enhanced Error Handling in EnsureAccessToken (Lines 374-392):**
```csharp
var response = await _httpClient.SendAsync(request);

if (!response.IsSuccessStatusCode)
{
	var errorContent = await response.Content.ReadAsStringAsync();
	throw new InvalidOperationException(
		$"Failed to obtain PayPal access token. Status: {response.StatusCode}, Error: {errorContent}");
}

var tokenResponse = await JsonSerializer.DeserializeAsync<PayPalTokenResponse>(
	await response.Content.ReadAsStreamAsync(),
	new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
	throw new InvalidOperationException("Failed to obtain PayPal access token - empty token response");
```

### 2. `WebApi/appsettings.json`

**Removed explicit `BaseUrl: null`** - now auto-defaults based on Environment setting

---

## 🎬 Next Steps to Continue

### Immediate Actions (Start Here Tomorrow)

1. **Get Detailed Error from PayPal**
   ```
   - Run shipment creation again
   - Check exception message for detailed PayPal error
   - Share error details: Status code + error JSON
   ```

2. **Verify PayPal Credentials**
   - Log into [PayPal Developer Dashboard](https://developer.paypal.com/dashboard)
   - Go to "Apps & Credentials" → Sandbox
   - Verify the app exists and is activated
   - Compare ClientId and ClientSecret in dashboard vs appsettings.json
   - Ensure they match EXACTLY (no extra spaces/characters)

3. **Common Fixes Based on Error**

   **If error is `invalid_client` (401):**
   - Credentials are wrong or for different environment
   - Copy fresh credentials from PayPal dashboard
   - Make sure using Sandbox credentials with `Environment: "Sandbox"`

   **If error is about app not activated:**
   - In PayPal dashboard, ensure Sandbox app status is "Live"
   - May need to click "Go Live" or activate the app

   **If credentials are correct but still failing:**
   - Try creating a NEW Sandbox app in PayPal dashboard
   - Use those fresh credentials
   - Sometimes old apps get deactivated

4. **Once OAuth Works**
   - Test full payment flow through shipment creation
   - Verify payment transaction is created in database
   - Check PayPal Sandbox dashboard for payment record

5. **Setup PayPal Webhook**
   - In PayPal Developer Dashboard → Webhooks
   - Create webhook with URL: `https://[NEW-NGROK-URL]/api/PaymentWebhooks/paypal`
	 - **Note:** ngrok URL changes each restart unless you have paid plan
   - Select event types:
	 - `PAYMENT.CAPTURE.COMPLETED`
	 - `PAYMENT.CAPTURE.REFUNDED`
	 - `PAYMENT.CAPTURE.DENIED`
   - Copy the Webhook ID from dashboard
   - Update `appsettings.json` with real WebhookId

6. **Test Webhook Delivery**
   - Make sure ngrok is running: `.\ngrok http 5228` from `E:\`
   - Create a test payment
   - Check ngrok web UI at `http://localhost:4040` to see webhook requests
   - Verify webhook is processed and marked in database

---

## 📚 Related Documentation Files

- **`PAYMENT_SYSTEM_COMPLETE.md`** - Complete payment system architecture and history
- **`.github/copilot-instructions.md`** - Project coding standards and guidelines
- **`WebApi.Tests/`** - Test suite for payment webhooks and reconciliation

---

## 🔑 Key Architectural Points

### Payment Flow
```
User creates shipment → ShipmentCommandService
  ↓
PaymentTransactionService.ProcessPayment()
  ↓
PaymentGatewayFactory.GetGatewayByIdAsync()
  ↓
PayPalPaymentGateway (or StripePaymentGateway)
  ↓
Process payment and return result
  ↓
If failed → Rollback shipment
If succeeded → Persist transaction
```

### Webhook Flow
```
PayPal sends webhook → https://[ngrok]/api/PaymentWebhooks/paypal
  ↓
PaymentWebhooksController.PayPal() endpoint
  ↓
Validate webhook signature using PayPal verification API
  ↓
Check if already processed (idempotency)
  ↓
Reconcile transaction in database
  ↓
Mark webhook as processed
```

### Database Tables
- `PaymentMethods` - Available payment methods (Visa, PayPal, etc.)
- `PaymentTransactions` - Payment attempts and results
- `WebhookEvents` - Received webhook events (deduplication)
- `Shipments` - Rollback if payment fails

---

## ⚠️ Important Reminders

1. **ngrok URL Changes:** Each time you restart ngrok, you get a new URL
   - Update PayPal webhook URL in dashboard each time
   - Or get ngrok paid plan for static domain

2. **Sandbox vs Live:** Never mix environments
   - Sandbox credentials only work with Sandbox URLs
   - Live credentials only work with Live URLs
   - Current setup is pure Sandbox

3. **Credentials Security:** 
   - Current credentials are in appsettings.json (OK for learning/testing)
   - For production, use Azure Key Vault or environment variables
   - Never commit secrets to source control in real projects

4. **WebhookId vs Webhook URL:**
   - Webhook URL: `https://[domain]/api/PaymentWebhooks/paypal` (where PayPal sends events)
   - Webhook ID: Unique identifier from PayPal dashboard (e.g., `01A68034B2206314R`)
   - They are NOT the same thing

5. **Debugger State:** 
   - Debugger is currently paused on exception
   - Need to stop/restart after code changes
   - New error handling will show detailed errors

---

## 🚀 Success Criteria

**Phase 1: OAuth Working** ✅ when:
- No exception during token acquisition
- Can obtain access token from PayPal
- Payment flow proceeds to order creation

**Phase 2: Payment Working** ✅ when:
- Shipment creation succeeds with PayPal payment method
- Payment transaction saved in database
- Payment visible in PayPal Sandbox dashboard

**Phase 3: Webhook Working** ✅ when:
- Webhook events delivered via ngrok
- Signature validation passes
- Transaction reconciliation occurs
- Webhook marked as processed (no duplicates)

**Phase 4: Production Ready** ✅ when:
- All tests passing
- Proper error handling
- Logging in place
- Documentation complete
- Ready to switch to Live environment

---

## 📞 Quick Reference Commands

### Start ngrok
```powershell
cd E:\
.\ngrok http 5228
```

### View ngrok requests
Open browser: `http://localhost:4040`

### Run WebApi
```powershell
cd E:\MyShipping\WebApi
dotnet run
```

### Run Tests
```powershell
cd E:\MyShipping
dotnet test WebApi.Tests/WebApi.Tests.csproj
```

### Check Database Migrations
```powershell
cd E:\MyShipping\DataAccessLayer
dotnet ef migrations list
dotnet ef database update
```

---

## 💡 Troubleshooting Guide

### If OAuth still fails after credential verification:
1. Create a fresh Sandbox app in PayPal dashboard
2. Use brand new credentials
3. Verify Environment is exactly "Sandbox" (case matters)
4. Test credentials with Postman/curl directly
5. Check PayPal API status page

### If webhook doesn't arrive:
1. Verify ngrok is running and shows correct forwarding URL
2. Check ngrok web UI (localhost:4040) for incoming requests
3. Verify webhook URL in PayPal dashboard matches current ngrok URL
4. Check PayPal dashboard webhook delivery history
5. Look for webhook events in ngrok traffic inspector

### If payment succeeds but shipment fails:
1. Check ShipmentCommandService rollback logic
2. Verify all required shipment fields are present
3. Check database constraints and validation
4. Review error logs for specific failure reason

---

## 📝 Session Notes

**Debugger Status:** Paused on exception in `PayPalPaymentGateway.EnsureAccessToken()`  
**Current File Open:** `Business/Services/PaymentGateways/PayPalPaymentGateway.cs`  
**Last Action:** Enhanced error handling to show detailed PayPal error responses  

**To resume:**
1. Read this file to get full context
2. Run shipment creation to get detailed error
3. Follow "Next Steps to Continue" section above
4. Reference PAYMENT_SYSTEM_COMPLETE.md for broader architecture context

---

## 🎓 Lessons Learned This Session

1. **Configuration Validation:** Always validate config strings with `IsNullOrWhiteSpace`, not just null checks
2. **Error Messages Matter:** Generic error messages hide the real problem; always include API error details
3. **Environment Matching:** Sandbox credentials only work with Sandbox URLs
4. **ngrok Limitations:** Free plan gives random URLs that change on restart
5. **Debugging Flow:** URI errors → OAuth errors → Payment errors (progressive debugging)

---

**End of Summary**  
**Next Session Start:** Read this file, then follow "Immediate Actions" in "Next Steps to Continue" section.
