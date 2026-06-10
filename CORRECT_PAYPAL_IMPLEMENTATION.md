# Correct PayPal Integration Implementation Guide

## ❌ What Was Wrong

The previous implementation used a **server-side redirect approach**:
1. Backend created PayPal order and returned approval URL
2. Frontend redirected user to PayPal website
3. User approved payment on PayPal
4. PayPal redirected back to backend callback
5. Backend captured payment and redirected to UI success page

**This approach is NOT recommended by PayPal and creates unnecessary complexity.**

---

## ✅ Correct Approach: PayPal JavaScript SDK

PayPal's recommended integration uses the **PayPal JavaScript SDK** on the frontend.

### How It Works:

```
┌─────────────────────────────────────────────────────────┐
│ 1. User Creates Shipment (No Payment Yet)              │
│    POST /api/Shipments/Create                           │
│    → Returns shipmentId                                 │
└─────────────────────────────────────────────────────────┘
					↓
┌─────────────────────────────────────────────────────────┐
│ 2. Frontend Shows PayPal Button                        │
│    (PayPal JS SDK renders button)                       │
│    User clicks PayPal button                            │
└─────────────────────────────────────────────────────────┘
					↓
┌─────────────────────────────────────────────────────────┐
│ 3. PayPal SDK Calls Your Backend                       │
│    createOrder() → POST /api/Payment/CreateOrder        │
│    {                                                     │
│      shipmentId: "guid",                                │
│      amount: 25.00                                      │
│    }                                                     │
│    ← Returns PayPal order ID                            │
└─────────────────────────────────────────────────────────┘
					↓
┌─────────────────────────────────────────────────────────┐
│ 4. PayPal SDK Shows Approval Popup                     │
│    (User logs in and approves - all in popup)          │
│    No page redirect!                                    │
└─────────────────────────────────────────────────────────┘
					↓
┌─────────────────────────────────────────────────────────┐
│ 5. PayPal SDK Calls Your Backend                       │
│    onApprove() → POST /api/Payment/CaptureOrder         │
│    {                                                     │
│      orderId: "paypal-order-id"                         │
│    }                                                     │
│    ← Returns capture result                             │
└─────────────────────────────────────────────────────────┘
					↓
┌─────────────────────────────────────────────────────────┐
│ 6. Frontend Shows Success Message                      │
│    (No page redirect needed)                            │
└─────────────────────────────────────────────────────────┘
```

---

## Implementation Steps

### Step 1: Create PaymentController in WebApi

Create `WebApi/Controllers/PaymentController.cs`:

```csharp
using Business.Contracts;
using Business.Services.PaymentGateways;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PaymentController : ControllerBase
	{
		private readonly IPaymentGatewayFactory _gatewayFactory;
		private readonly IPaymentTransactionService _paymentTransactionService;
		private readonly ILogger<PaymentController> _logger;

		public PaymentController(
			IPaymentGatewayFactory gatewayFactory,
			IPaymentTransactionService paymentTransactionService,
			ILogger<PaymentController> logger)
		{
			_gatewayFactory = gatewayFactory;
			_paymentTransactionService = paymentTransactionService;
			_logger = logger;
		}

		/// <summary>
		/// Creates a PayPal order (called by PayPal JS SDK)
		/// </summary>
		[HttpPost("CreateOrder")]
		public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
		{
			try
			{
				var gateway = await _gatewayFactory.GetGatewayByNameAsync("PayPal");
				if (gateway is not PayPalPaymentGateway paypalGateway)
				{
					return BadRequest(new { error = "PayPal gateway not available" });
				}

				// Create PayPal order (don't capture yet)
				var result = await paypalGateway.ProcessPayment(
					request.Amount,
					"USD",
					null, // No token needed for order creation
					request.ShipmentId
				);

				if (result.Success)
				{
					// Return order ID to PayPal SDK
					return Ok(new { orderId = result.TransactionId });
				}

				return BadRequest(new { error = result.ErrorMessage });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating PayPal order");
				return StatusCode(500, new { error = "Failed to create order" });
			}
		}

		/// <summary>
		/// Captures a PayPal order after user approval (called by PayPal JS SDK)
		/// </summary>
		[HttpPost("CaptureOrder")]
		public async Task<IActionResult> CaptureOrder([FromBody] CaptureOrderRequest request)
		{
			try
			{
				var gateway = await _gatewayFactory.GetGatewayByNameAsync("PayPal");
				if (gateway is not PayPalPaymentGateway paypalGateway)
				{
					return BadRequest(new { error = "PayPal gateway not available" });
				}

				// Capture the approved order
				var result = await paypalGateway.CaptureOrder(request.OrderId);

				if (result.Success)
				{
					// Update transaction in database
					await _paymentTransactionService.ReconcileTransactionFromCallback(
						request.OrderId,
						result
					);

					return Ok(new
					{
						success = true,
						captureId = result.TransactionId,
						message = "Payment captured successfully"
					});
				}

				return BadRequest(new { error = result.ErrorMessage });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error capturing PayPal order");
				return StatusCode(500, new { error = "Failed to capture payment" });
			}
		}
	}

	public class CreateOrderRequest
	{
		public Guid ShipmentId { get; set; }
		public decimal Amount { get; set; }
	}

	public class CaptureOrderRequest
	{
		public string OrderId { get; set; }
	}
}
```

---

### Step 2: Add PayPal JS SDK to Frontend

In your shipment creation or payment page (`UI/Views/Shipments/Create.cshtml` or similar):

```html
@section Scripts {
	<!-- PayPal JavaScript SDK -->
	<script src="https://www.paypal.com/sdk/js?client-id=YOUR_CLIENT_ID&currency=USD"></script>

	<script>
		// Initialize PayPal Button
		paypal.Buttons({
			// Called when user clicks PayPal button
			createOrder: function(data, actions) {
				// Call your backend to create PayPal order
				return fetch('/api/Payment/CreateOrder', {
					method: 'POST',
					headers: {
						'Content-Type': 'application/json',
						'Authorization': 'Bearer ' + getJwtToken() // If using JWT
					},
					body: JSON.stringify({
						shipmentId: '@Model.ShipmentId', // Or get from form
						amount: parseFloat(document.getElementById('shippingRate').value)
					})
				})
				.then(function(response) {
					return response.json();
				})
				.then(function(data) {
					// Return the order ID to PayPal SDK
					return data.orderId;
				});
			},

			// Called when user approves payment
			onApprove: function(data, actions) {
				// Call your backend to capture payment
				return fetch('/api/Payment/CaptureOrder', {
					method: 'POST',
					headers: {
						'Content-Type': 'application/json',
						'Authorization': 'Bearer ' + getJwtToken()
					},
					body: JSON.stringify({
						orderId: data.orderID
					})
				})
				.then(function(response) {
					return response.json();
				})
				.then(function(captureData) {
					if (captureData.success) {
						// Show success message
						alert('Payment successful! Capture ID: ' + captureData.captureId);
						// Redirect to shipments list or success page
						window.location.href = '/Shipments/List?created=1';
					} else {
						alert('Payment failed: ' + captureData.error);
					}
				});
			},

			// Called when user cancels
			onCancel: function(data) {
				alert('Payment cancelled by user');
			},

			// Called on error
			onError: function(err) {
				console.error('PayPal error:', err);
				alert('An error occurred during payment');
			}
		}).render('#paypal-button-container');
	</script>
}

<!-- PayPal button container -->
<div id="paypal-button-container"></div>
```

---

### Step 3: Update PayPalPaymentGateway

The existing `PayPalPaymentGateway` needs to be modified:

1. **`ProcessPayment` method** should create order WITHOUT capturing
2. **`CaptureOrder` method** should capture an already-approved order

You may already have these methods. If not, here's the structure:

```csharp
// In PayPalPaymentGateway.cs

public async Task<PaymentResult> ProcessPayment(
	decimal amount,
	string currency,
	string? paymentMethodToken,
	Guid? referenceId = null)
{
	// Create order but DON'T capture
	var orderRequest = new
	{
		intent = "CAPTURE",
		purchase_units = new[]
		{
			new
			{
				reference_id = referenceId?.ToString(),
				amount = new
				{
					currency_code = currency,
					value = amount.ToString("F2")
				}
			}
		}
	};

	// Call PayPal Orders API
	var response = await _httpClient.PostAsJsonAsync("/v2/checkout/orders", orderRequest);

	if (response.IsSuccessStatusCode)
	{
		var order = await response.Content.ReadFromJsonAsync<PayPalOrder>();

		return new PaymentResult
		{
			Success = true,
			TransactionId = order.id, // Return order ID
			TransactionStatus = PaymentTransactionStatus.Pending
		};
	}

	return new PaymentResult { Success = false, ErrorMessage = "Failed to create order" };
}

public async Task<PaymentResult> CaptureOrder(string orderId)
{
	// Capture the approved order
	var response = await _httpClient.PostAsync(
		$"/v2/checkout/orders/{orderId}/capture",
		null
	);

	if (response.IsSuccessStatusCode)
	{
		var capture = await response.Content.ReadFromJsonAsync<PayPalCapture>();

		return new PaymentResult
		{
			Success = true,
			TransactionId = capture.purchase_units[0].payments.captures[0].id,
			TransactionStatus = PaymentTransactionStatus.Completed,
			ProcessedAt = DateTime.UtcNow
		};
	}

	return new PaymentResult { Success = false, ErrorMessage = "Failed to capture payment" };
}
```

---

## Benefits of This Approach

✅ **No page redirects** - Better user experience
✅ **Popup approval** - User never leaves your site
✅ **PayPal handles UI** - No need to build PayPal login/approval pages
✅ **Simpler backend** - No callback endpoints needed
✅ **More secure** - PayPal SDK handles security
✅ **Better mobile support** - Works seamlessly on mobile devices

---

## Configuration

### appsettings.json

```json
{
  "PaymentGateways": {
	"PayPal": {
	  "Enabled": true,
	  "ClientId": "YOUR_SANDBOX_CLIENT_ID",
	  "ClientSecret": "YOUR_SANDBOX_SECRET",
	  "Environment": "Sandbox"
	}
  }
}
```

### Frontend JavaScript

Replace `YOUR_CLIENT_ID` in the PayPal SDK script tag with your actual Client ID from `appsettings.json`.

---

## Testing

1. **Create shipment** - Should succeed without payment
2. **Click PayPal button** - PayPal popup should appear
3. **Login to PayPal Sandbox** - Use test buyer account
4. **Approve payment** - In the popup
5. **Popup closes** - Should show success message
6. **Check database** - Transaction should be Completed

---

## Next Steps

1. Create `PaymentController.cs` with CreateOrder and CaptureOrder endpoints
2. Add PayPal JS SDK to your payment page
3. Implement PayPal button rendering
4. Test the complete flow
5. Remove commented-out code once new approach is working

---

## References

- [PayPal JavaScript SDK Documentation](https://developer.paypal.com/sdk/js/)
- [PayPal Orders API](https://developer.paypal.com/docs/api/orders/v2/)
- [PayPal Integration Guide](https://developer.paypal.com/docs/checkout/standard/integrate/)

---

## Summary

**Old Approach (Removed):**
- Server-side redirect
- User leaves site
- Complex callback handling
- Multiple page redirects

**New Approach (To Implement):**
- Client-side PayPal SDK
- User stays on site (popup)
- Simple API endpoints
- Better user experience

The foundation is ready - you just need to create the PaymentController and add the PayPal JS SDK to your frontend!
