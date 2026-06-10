# PayPal Integration - Complete Testing Guide

## Architecture Overview

Your application has **TWO separate projects**:

1. **UI Project** (MVC Frontend)
   - URL: `https://localhost:7065`
   - Contains: Views, Controllers, JavaScript for user interface
   - **Shipment creation form is here**

2. **WebApi Project** (Backend API)
   - URL: `https://localhost:7228`
   - Contains: API endpoints, business logic, payment processing
   - **Actual shipment creation happens here**

### The Flow:

```
User on UI (7065)
	↓
JavaScript calls API (7228)
	↓
API creates shipment + PayPal order
	↓
API returns response WITH payment transaction
	↓
JavaScript detects PayPal approval URL
	↓
JavaScript redirects user to PayPal
	↓
User approves on PayPal
	↓
PayPal redirects to WebApi callback (7228)
	↓
WebApi captures payment
	↓
WebApi redirects to UI Success page (7065)
```

---

## Prerequisites - BOTH Projects Must Be Running

### Important: You MUST run BOTH projects simultaneously!

### Option 1: Multiple Startup Projects (Recommended)

1. Right-click on **Solution** in Solution Explorer
2. Select **Properties**
3. Go to **Startup Project**
4. Select **Multiple startup projects**
5. Set both **UI** and **WebApi** to **Start**
6. Click **OK**

Now when you press F5, both projects will start together.

### Option 2: Manual Start (Alternative)

1. Start **WebApi** first (F5 or Ctrl+F5)
2. Right-click **UI** project → **Debug** → **Start New Instance**

---

## Step-by-Step Testing Instructions

### 1. Stop Current Debug Session

- Press Shift+F5 or click Stop button
- **CRITICAL**: The code changes won't take effect until you restart!

### 2. Start Both Projects

- Press F5 to start debugging
- **Verify both projects are running:**
  - WebApi should open: `https://localhost:7228/swagger`
  - UI should open: `https://localhost:7065`

### 3. Navigate to Shipment Creation

Go to: `https://localhost:7065/Shipments/Create`

### 4. Fill Out the Form

**Sender Information:**
- Country: United States
- Name: John Sender
- Contact: +1234567890
- Address: 123 Main St
- Postal Code: 12345
- City: (select any)

**Receiver Information:**
- Country: Canada
- Name: Jane Receiver
- Contact: +0987654321
- Address: 456 Oak Ave
- Postal Code: A1B2C3
- City: (select any)

**Package Details:**
- Width: 10
- Height: 10
- Weight: 5
- Length: 10
- Package Value: 100
- Shipping Type: (select any)
- Shipping Package: (select any)

**Payment:**
- **Payment Method: Select "PayPal"** ⬅️ IMPORTANT!
- Shipping Rate: 25.00

### 5. Open Browser Console

**CRITICAL FOR DEBUGGING:**
- Press F12 to open Developer Tools
- Go to **Console** tab
- Keep it open to see debug messages

### 6. Submit the Form

Click "Submit" or "Create Shipment" button

### 7. What Should Happen

#### ✅ **Expected Behavior:**

**In Browser Console, you should see:**
```
SubmitSHipment Called in the Console From the ShipmentService.js
ShipmentService.create resp: {isSuccess: true, data: {...}, ...}
Payment transaction detected: {status: 0, additionalInfo: "https://www.sandbox.paypal.com/..."}
PayPal approval required - redirecting to: https://www.sandbox.paypal.com/checkoutnow?token=...
```

**Then:**
1. Toast message: "Redirecting to PayPal for payment approval..."
2. **Browser automatically redirects to PayPal Sandbox**
3. PayPal login page appears

#### ❌ **If Redirect Doesn't Happen:**

**Check Console for errors:**

**Scenario A: No payment transaction in response**
```
ShipmentService.create resp: {isSuccess: true, data: null}
```
**Problem:** API not returning shipment data
**Solution:** Check next section "Troubleshooting"

**Scenario B: Transaction but no AdditionalInfo**
```
Payment transaction detected: {status: 0, additionalInfo: null}
```
**Problem:** PayPal order not created or approval URL missing
**Solution:** Check WebApi logs

**Scenario C: JavaScript error**
```
TypeError: Cannot read property 'PaymentTransaction' of null
```
**Problem:** Response structure doesn't match expected format
**Solution:** Check Network tab response

### 8. On PayPal Sandbox

**Login with Sandbox Account:**
- You need a PayPal Sandbox **buyer** account
- Go to: https://developer.paypal.com/dashboard/accounts
- Find your test buyer account credentials
- Or create one if you don't have it

**After Login:**
1. You'll see payment amount ($25.00)
2. Click **"Pay Now"** or **"Continue"**
3. **DO NOT** click "Cancel" for this first test

### 9. Return to Your Application

**After clicking "Pay Now" on PayPal:**

1. PayPal redirects to: `https://localhost:7228/api/PayPalCallback/return?token=...`
2. WebApi captures the payment
3. WebApi redirects to: `https://localhost:7065/Shipments/Success?shipmentId=...`
4. **You should see the Success page** with:
   - ✅ Green checkmark
   - Success message
   - Shipment details
   - Buttons to view shipment or create new one

### 10. Verify in Database

```sql
-- Check the payment transaction
SELECT TOP 1 
	Id,
	ShipmentId,
	ProviderName,
	TransactionStatus,  -- Should be 1 (Completed)
	TransactionReference,  -- PayPal capture ID
	AdditionalInfo,  -- Original approval URL
	ProcessedDate,  -- Should have timestamp
	CreatedDate
FROM TbPaymentTransactions
ORDER BY CreatedDate DESC
```

**Expected Result:**
- `TransactionStatus`: 1 (Completed)
- `TransactionReference`: Capture ID (e.g., "5AB12CD34...")
- `ProcessedDate`: Current timestamp
- `AdditionalInfo`: The PayPal approval URL

---

## Testing Cancellation Flow

### 1. Create Another Shipment

Follow steps 1-6 above, select PayPal payment

### 2. On PayPal Page

Click **"Cancel and Return to MyShipping"** button

### 3. Expected Behavior

1. PayPal redirects to: `https://localhost:7228/api/PayPalCallback/cancel`
2. WebApi redirects to: `https://localhost:7065/Shipments/Cancel?message=...`
3. **You should see Cancel page** with:
   - ⚠️ Warning icon
   - "Payment Cancelled" message
   - Helpful suggestions
   - "Try Again" button

---

## Troubleshooting

### Problem: Redirect to PayPal Doesn't Happen

#### Step 1: Check Browser Console

Look for the debug messages. You should see:
```
ShipmentService.create resp: {...}
Payment transaction detected: {...}
```

**If you DON'T see these messages:**
- JavaScript isn't running or has an error
- Check Console for red error messages

#### Step 2: Check Network Tab

1. Open F12 → **Network** tab
2. Look for `POST /api/Shipments/Create` request
3. Click on it
4. Go to **Response** tab

**Expected Response:**
```json
{
  "isSuccess": true,
  "message": "Shipment Created Successfully",
  "data": {
	"id": "some-guid",
	"trackingNumber": 123456,
	"paymentTransaction": {
	  "id": "guid",
	  "shipmentId": "guid",
	  "transactionStatus": 0,
	  "additionalInfo": "https://www.sandbox.paypal.com/checkoutnow?token=..."
	}
  }
}
```

**If `data` is null or missing `paymentTransaction`:**
- The API changes didn't take effect
- You need to **restart the WebApi project**

#### Step 3: Check WebApi Is Running

- Navigate to: `https://localhost:7228/swagger`
- If it doesn't open, WebApi isn't running
- Make sure you started BOTH projects (see Prerequisites)

#### Step 4: Check WebApi Logs

In Visual Studio:
1. Look at **Output** window
2. Select **Debug** from dropdown
3. Look for errors during shipment creation

**Common Errors:**
```
Failed to obtain PayPal access token
```
**Solution:** Check PayPal credentials in appsettings.json

```
Payment gateway not found
```
**Solution:** Check PaymentGateways:PayPal:Enabled = true

### Problem: Error When Creating Shipment

#### Check the Error Message

**If you see:**
```
Payment failed: ...
```
**Possible causes:**
1. Invalid PayPal credentials
2. PayPal Sandbox is down
3. Network connectivity issues

**Check WebApi logs for detailed error**

### Problem: Success/Cancel Pages Not Found (404)

**Error:** Cannot find `/Shipments/Success` or `/Shipments/Cancel`

**Cause:** The UI project isn't running or the routes aren't registered

**Solution:**
1. Make sure UI project is running on port 7065
2. Check that Success.cshtml and Cancel.cshtml exist in `UI/Views/Shipments/`
3. Check ShipmentsController has Success and Cancel action methods

---

## Verify Configuration

### WebApi appsettings.json

```json
{
  "ClientApp": {
	"BaseUrl": "https://localhost:7065"  // ← Must match UI port
  },
  "PaymentGateways": {
	"PayPal": {
	  "Enabled": true,  // ← Must be true
	  "ClientId": "your-sandbox-client-id",
	  "ClientSecret": "your-sandbox-secret",
	  "Environment": "Sandbox"  // ← Must be Sandbox for testing
	}
  }
}
```

### Check Both Projects Are in Solution

In Solution Explorer, you should see:
- ✅ UI (project)
- ✅ WebApi (project)
- ✅ Business (project)
- ✅ DataAccessLayer (project)
- ✅ Domains (project)

---

## Complete Test Checklist

Before testing, verify:

- [ ] Both UI and WebApi projects are set to start
- [ ] Previous debug session is stopped
- [ ] Browser console is open (F12)
- [ ] PayPal credentials are correct in appsettings.json
- [ ] You have PayPal Sandbox buyer account credentials

During test:

- [ ] Form submits successfully
- [ ] Console shows payment transaction detected
- [ ] Console shows "redirecting to PayPal"
- [ ] Browser redirects to PayPal Sandbox
- [ ] Can login to PayPal
- [ ] Can approve payment
- [ ] Redirects back to Success page
- [ ] Success page shows shipment details
- [ ] Database shows transaction status = 1

---

## What to Report If It Doesn't Work

If the redirect still doesn't happen, please provide:

1. **Console Output** (copy from F12 Console tab)
2. **Network Response** (from Network tab → POST Create → Response)
3. **WebApi Logs** (from Visual Studio Output window)
4. **Any Error Messages** you see

This will help me identify the exact issue!

---

## Important Notes

### Why Two Projects?

Your application uses a **separated frontend/backend architecture**:
- **UI**: Handles presentation, forms, views
- **WebApi**: Handles business logic, database, external APIs

This is a modern, scalable architecture but requires both projects to run simultaneously.

### CORS Configuration

If you get CORS errors, the WebApi needs to allow requests from the UI origin. This should already be configured, but if you see CORS errors in console, let me know.

### ngrok for PayPal Webhooks

For webhooks to work (asynchronous payment notifications), you need ngrok running. But for the basic redirect flow, ngrok is **NOT required**. We're testing the synchronous redirect flow first.

---

## Success Criteria

✅ **Test is successful when:**

1. User submits form with PayPal selected
2. Browser automatically redirects to PayPal
3. User can log in and approve payment
4. Browser returns to Success page
5. Database shows completed transaction
6. No errors in console or logs

Once this works, the PayPal integration is fully functional!
