# PayPal Payment Flow - Important Changes

## 🔄 What Changed and Why

### The Problem You Encountered

**Error:** `ORDER_NOT_APPROVED` - "Payer has not yet approved the Order for payment"

**Why This Happened:**
Your code was trying to capture payment immediately after creating the order, but PayPal requires 
user approval first for standard e-commerce payments.

---

## 📊 How PayPal Payments Actually Work

### Two PayPal Payment Models:

#### 1. **Server-to-Server (Direct Payment)** ❌ Not suitable for your use case
- Requires stored payment methods or vaulted payment tokens
- Needs `payment_source` in request
- Used for: Subscriptions, saved payment methods

#### 2. **Redirect Flow (Standard E-Commerce)** ✅ What we implemented
- User approves payment on PayPal's website
- Requires user redirect and approval
- Used for: One-time payments, shopping carts, shipments

---

## 🔄 Correct PayPal Flow (What We Implemented)

```
Step 1: User creates shipment
   ↓
Step 2: Backend creates PayPal order (ProcessPayment)
   ↓
Step 3: Return approval URL to frontend
   ↓
Step 4: Redirect user to PayPal
   ↓
Step 5: User logs into PayPal and approves payment
   ↓
Step 6: PayPal redirects user back to your site
   ↓
Step 7: Backend captures the order (CaptureOrder)
   ↓
Step 8: Payment complete! ✅
```

---

## 💻 Code Changes Made

### 1. **Updated ProcessPayment Method**

**Before (Wrong):**
```csharp
// Created order AND tried to capture immediately
if (request.CaptureImmediately) {
	await CaptureOrder();  // ❌ FAILS - User hasn't approved!
}
```

**After (Correct):**
```csharp
// Create order and return approval URL
var approvalUrl = orderResponse.Links
	.FirstOrDefault(link => link.Rel == "approve")
	?.Href;

return new PaymentResult {
	Status = PaymentStatus.RequiresAction,  // ✅ Needs user approval
	AdditionalInfo = approvalUrl             // ✅ Frontend redirects here
};
```

### 2. **Added CaptureOrder Method**

New method to call AFTER user approves:

```csharp
public async Task<PaymentResult> CaptureOrder(string orderId)
{
	// Capture the approved order
	var captureResponse = await PostAsync<PayPalCaptureResponse>(
		$"/v2/checkout/orders/{orderId}/capture", 
		new { });

	// Return success/failure
}
```

### 3. **Updated PayPalOrderResponse Model**

Added `Links` property to get approval URL:

```csharp
private class PayPalOrderResponse
{
	public string Id { get; set; }
	public string Status { get; set; }
	public List<PayPalLink>? Links { get; set; }  // ✅ NEW
}

private class PayPalLink
{
	public string Href { get; set; }  // The URL
	public string Rel { get; set; }   // "approve", "capture", etc.
	public string Method { get; set; } // "GET", "POST", etc.
}
```

---

## 🚨 IMPORTANT: What You Need to Do Now

### Your Current Shipment Flow Will Break! ❌

**Why:** The payment now returns `RequiresAction` instead of `Completed`,
so your shipment creation logic needs updates.

### Current Shipment Creation Logic:

```csharp
// Business/Services/Shipment/ShipmentCommandService.cs
var paymentTransaction = await _paymentTransactionService.ProcessPayment(...);

if (paymentTransaction.TransactionStatus == 2) {  // Failed
	throw new Exception("Payment failed");
}

// ❌ PROBLEM: Payment is now "RequiresAction", not "Completed"!
// Shipment gets created, but payment isn't captured yet!
```

---

## ✅ What Needs to Be Implemented

You have **two options**:

### **Option A: Async Payment (Recommended for PayPal)**

1. **Create shipment with PENDING payment**
2. **Return approval URL to user**
3. **User approves on PayPal**
4. **Callback endpoint captures order and updates shipment**

### **Option B: Synchronous Payment (Complex)**

1. **Show PayPal popup/redirect during shipment creation**
2. **Wait for user to approve**
3. **Capture immediately after approval**
4. **Complete shipment creation**

---

## 🎯 Recommended Implementation (Option A)

### Step 1: Update ShipmentCommandService

Allow shipments with pending PayPal payments:

```csharp
var paymentTransaction = await _paymentTransactionService.ProcessPayment(...);

if (paymentTransaction.TransactionStatus == (int)PaymentTransactionStatus.Failed) {
	throw new Exception("Payment failed");
}

// ✅ Allow RequiresAction status for PayPal
if (paymentTransaction.TransactionStatus == (int)PaymentTransactionStatus.RequiresAction) {
	// Store approval URL, return to frontend
	return new {
		ShipmentId = createdId,
		PaymentApprovalRequired = true,
		ApprovalUrl = paymentTransaction.AdditionalInfo  // PayPal approval URL
	};
}
```

### Step 2: Create PayPal Callback Controller

```csharp
[Route("api/[controller]")]
[ApiController]
public class PayPalCallbackController : ControllerBase
{
	private readonly IPaymentTransactionService _paymentService;
	private readonly IPaymentGatewayFactory _gatewayFactory;

	[HttpGet("return")]
	public async Task<IActionResult> Return([FromQuery] string token)
	{
		// token = PayPal order ID

		// 1. Get PayPal gateway
		var gateway = await _gatewayFactory.GetGatewayByNameAsync("PayPal");
		var paypalGateway = gateway as PayPalPaymentGateway;

		// 2. Capture the approved order
		var captureResult = await paypalGateway.CaptureOrder(token);

		// 3. Update payment transaction in database
		await _paymentService.ReconcileTransactionFromCallback(token, captureResult);

		// 4. Redirect user to shipment success page
		return Redirect($"/Shipments/Success?orderId={token}");
	}

	[HttpGet("cancel")]
	public IActionResult Cancel()
	{
		// User cancelled payment on PayPal
		return Redirect("/Shipments/Create?error=payment_cancelled");
	}
}
```

### Step 3: Update Frontend

```csharp
// After shipment create API call:
if (response.PaymentApprovalRequired) {
	// Redirect user to PayPal
	window.location.href = response.ApprovalUrl;
} else {
	// Payment complete, show success
	window.location.href = "/Shipments/Success";
}
```

---

## 📝 Configuration Required

### Update appsettings.json

Add return URLs for PayPal callbacks:

```json
{
  "PaymentGateways": {
	"PayPal": {
	  "ReturnUrl": "https://localhost:7228/api/PayPalCallback/return",
	  "CancelUrl": "https://localhost:7228/api/PayPalCallback/cancel"
	}
  }
}
```

### For Local Testing with ngrok

```json
{
  "PaymentGateways": {
	"PayPal": {
	  "ReturnUrl": "https://[your-ngrok-url]/api/PayPalCallback/return",
	  "CancelUrl": "https://[your-ngrok-url]/api/PayPalCallback/cancel"
	}
  }
}
```

---

## 🧪 Testing the New Flow

### 1. Create Shipment with PayPal

```
POST /api/Shipments/Create
{
  "PaymentMethodId": "...",  // PayPal method
  ...
}

Response:
{
  "ShipmentId": "...",
  "PaymentApprovalRequired": true,
  "ApprovalUrl": "https://www.sandbox.paypal.com/checkoutnow?token=5O190127TN364715T"
}
```

### 2. Frontend Redirects User

User goes to PayPal, logs in, approves payment.

### 3. PayPal Redirects Back

```
GET https://localhost:7228/api/PayPalCallback/return?token=5O190127TN364715T
```

### 4. Backend Captures Payment

Your callback controller:
- Calls `CaptureOrder(token)`
- Updates database
- Redirects user to success page

---

## 🔍 How to Check if It's Working

### 1. **PayPal Developer Dashboard**

- Go to: https://developer.paypal.com/dashboard/
- Sandbox → Orders
- You'll see order with status "CREATED" (before approval)
- After user approves: status changes to "COMPLETED"

### 2. **Your Database**

```sql
-- Check payment transactions
SELECT 
	TransactionReference,  -- PayPal order ID
	TransactionStatus,     -- 3 = RequiresAction, 1 = Completed
	Notes,
	ProcessedDate
FROM PaymentTransactions
ORDER BY CreatedDate DESC;
```

### 3. **ngrok Web UI**

- http://localhost:4040
- See the callback request when PayPal redirects back

---

## ⚠️ Important Notes

### 1. **Order Expiration**

PayPal orders expire after **3 hours** if not approved.  
You may want to clean up pending shipments after expiration.

### 2. **Security**

Always verify the order belongs to the user before capturing:

```csharp
var transaction = await _repository.GetByTransactionReference(orderId);
if (transaction.ShipmentId != userShipmentId) {
	return Unauthorized();
}
```

### 3. **Webhooks Still Important**

Even with callbacks, webhooks are needed for:
- Refunds
- Chargebacks
- Disputes
- Delayed notifications

---

## 🎯 Next Steps (In Order)

1. **Stop Debugger** - Code changes won't apply while debugging
2. **Decide on Option A or B** - Async payment (recommended) or sync?
3. **Update ShipmentCommandService** - Handle `RequiresAction` status
4. **Create PayPal Callback Controller** - Handle return/cancel
5. **Update Frontend** - Redirect to approval URL
6. **Test End-to-End** - Create shipment → approve on PayPal → verify capture
7. **Check Database** - Verify transaction updated to Completed

---

## 📚 Additional Resources

- [PayPal Orders API](https://developer.paypal.com/docs/api/orders/v2/)
- [PayPal Checkout Integration](https://developer.paypal.com/docs/checkout/)
- [Testing in Sandbox](https://developer.paypal.com/tools/sandbox/)

---

**Questions? Need help implementing the callback flow? Let me know!** 🚀
