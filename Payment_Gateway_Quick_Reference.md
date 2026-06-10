# Payment Gateway Quick Reference

## 🚀 Quick Start

### 1. Configure Your Credentials

Edit `UI/appsettings.json`:

```json
{
  "PaymentGateways": {
	"DefaultGateway": "Stripe",
	"Currency": "USD",
	"CaptureMethod": "automatic",
	"Stripe": {
	  "Enabled": true,
	  "PublishableKey": "pk_test_YOUR_STRIPE_KEY",
	  "SecretKey": "sk_test_YOUR_STRIPE_KEY",
	  "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET",
	  "Environment": "Test"
	},
	"PayPal": {
	  "Enabled": true,
	  "ClientId": "YOUR_PAYPAL_CLIENT_ID",
	  "ClientSecret": "YOUR_PAYPAL_CLIENT_SECRET",
	  "WebhookId": "YOUR_PAYPAL_WEBHOOK_ID",
	  "Environment": "Sandbox"
	}
  }
}
```

### 2. Get Test Credentials

**Stripe Test Mode:**
- Dashboard: https://dashboard.stripe.com/test
- Test Card: `4242 4242 4242 4242`, any future date, any CVC

**PayPal Sandbox:**
- Dashboard: https://developer.paypal.com/dashboard/
- Test Account: Create in Sandbox Accounts section

---

## 📝 Code Examples

### Process a Payment

```csharp
public class PaymentController : Controller
{
	private readonly IPaymentTransactionService _paymentService;

	public PaymentController(IPaymentTransactionService paymentService)
	{
		_paymentService = paymentService;
	}

	[HttpPost]
	public async Task<IActionResult> ProcessPayment(Guid shipmentId, Guid paymentMethodId)
	{
		try
		{
			// Calculate shipping rate (your existing logic)
			var shippingRate = 50.00m;

			// Process payment through appropriate gateway
			var result = await _paymentService.ProcessPayment(
				shipmentId,
				paymentMethodId,
				shippingRate
			);

			if (result.TransactionStatusName == "Completed")
			{
				return Ok(new { 
					success = true, 
					message = "Payment successful",
					transactionId = result.TransactionReference
				});
			}
			else
			{
				return BadRequest(new { 
					success = false, 
					message = result.Notes 
				});
			}
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { 
				success = false, 
				message = ex.Message 
			});
		}
	}
}
```

### Process a Refund

```csharp
[HttpPost]
public async Task<IActionResult> ProcessRefund(Guid transactionId, string reason)
{
	try
	{
		var result = await _paymentService.SimulateRefund(
			transactionId,
			reason ?? "Customer requested refund"
		);

		if (result.TransactionStatusName == "Refunded")
		{
			return Ok(new { 
				success = true, 
				message = "Refund successful" 
			});
		}
		else
		{
			return BadRequest(new { 
				success = false, 
				message = "Refund failed" 
			});
		}
	}
	catch (Exception ex)
	{
		return StatusCode(500, new { 
			success = false, 
			message = ex.Message 
		});
	}
}
```

### Directly Use a Specific Gateway

```csharp
public class DirectGatewayExample
{
	private readonly IPaymentGatewayFactory _gatewayFactory;

	public DirectGatewayExample(IPaymentGatewayFactory gatewayFactory)
	{
		_gatewayFactory = gatewayFactory;
	}

	public async Task<PaymentResult> ProcessStripePayment(decimal amount)
	{
		// Get Stripe gateway specifically
		var stripeGateway = _gatewayFactory.GetGateway("CreditCard");

		// Build payment request
		var request = new PaymentRequest
		{
			Amount = amount,
			Currency = "USD",
			Description = "Test payment",
			Customer = new CustomerInfo
			{
				Email = "customer@example.com",
				Name = "John Doe"
			},
			CaptureImmediately = true
		};

		// Process payment
		return await stripeGateway.ProcessPayment(request);
	}

	public async Task<PaymentResult> ProcessPayPalPayment(decimal amount)
	{
		// Get PayPal gateway specifically
		var paypalGateway = _gatewayFactory.GetGateway("PayPal");

		// Build payment request
		var request = new PaymentRequest
		{
			Amount = amount,
			Currency = "USD",
			Description = "Test payment",
			Customer = new CustomerInfo
			{
				Email = "customer@example.com",
				Name = "Jane Doe"
			},
			CaptureImmediately = true
		};

		// Process payment
		return await paypalGateway.ProcessPayment(request);
	}
}
```

---

## 🏗️ Architecture Components

### Core Abstractions

```
Business/
├── Contracts/
│   ├── IPaymentGateway.cs           # Core gateway interface
│   └── IPaymentGatewayFactory.cs    # Factory interface
├── Models/
│   ├── PaymentRequest.cs            # Input model
│   ├── PaymentResult.cs             # Output model
│   ├── RefundRequest.cs             # Refund input
│   ├── RefundResult.cs              # Refund output
│   └── CustomerInfo.cs              # Customer data
├── Configuration/
│   └── PaymentGatewayOptions.cs     # Configuration models
└── Services/
	├── PaymentTransactionService.cs # Main service (updated)
	└── PaymentGateways/
		├── StripePaymentGateway.cs  # Stripe implementation
		├── PayPalPaymentGateway.cs  # PayPal implementation
		└── PaymentGatewayFactory.cs # Gateway selector
```

### Gateway Selection Logic

The `PaymentGatewayFactory` automatically selects the correct gateway based on payment method name:

| Payment Method Name | Selected Gateway |
|---------------------|------------------|
| CreditCard          | Stripe           |
| Credit Card         | Stripe           |
| Card                | Stripe           |
| Visa                | Stripe           |
| MasterCard          | Stripe           |
| PayPal              | PayPal           |
| *Any other*         | Stripe (default) |

You can also select by database ID using `GetGatewayByIdAsync(Guid paymentMethodId)`.

---

## 🔍 Payment Status Flow

### Status Mapping

| Gateway Status | Internal Status | Description |
|----------------|-----------------|-------------|
| Stripe: `succeeded` | `Completed` | Payment successful |
| Stripe: `requires_action` | `RequiresAction` | 3D Secure needed |
| Stripe: `processing` | `Pending` | Still processing |
| Stripe: `canceled` | `Canceled` | Payment canceled |
| PayPal: `COMPLETED` | `Completed` | Payment successful |
| PayPal: `CREATED` | `Pending` | Order created, not captured |
| PayPal: `APPROVED` | `RequiresAction` | Approved, needs capture |

### Transaction Status Enum

```csharp
public enum PaymentTransactionStatus
{
	Pending = 0,
	Completed = 1,
	Failed = 2,
	Refunded = 3,
	PartiallyRefunded = 4
}
```

---

## 🧪 Testing

### Test Cards (Stripe)

| Card Number | Description | Result |
|-------------|-------------|--------|
| 4242 4242 4242 4242 | Visa | Success |
| 4000 0000 0000 9995 | Visa | Declined |
| 4000 0027 6000 3184 | Visa | Requires 3D Secure |

Use any future expiry date and any 3-digit CVC.

### Test PayPal Accounts

Create test buyer and seller accounts in PayPal Sandbox:
1. Go to https://developer.paypal.com/dashboard/
2. Navigate to "Sandbox" > "Accounts"
3. Create test accounts
4. Use test credentials in your integration

---

## 🐛 Troubleshooting

### Common Issues

#### 1. "Payment gateway API key not configured"
**Solution:** Check `appsettings.json` for correct `SecretKey` (Stripe) or `ClientId/ClientSecret` (PayPal).

#### 2. "Payment failed: Authentication failed"
**Solution:** Verify your API keys are correct and match the environment (Test/Sandbox vs Live).

#### 3. "PayPal order creation failed"
**Solution:** Ensure `Environment` is set to `Sandbox` for testing and `BaseUrl` is correct or null.

#### 4. "Webhook validation failed"
**Solution:** Check `WebhookSecret` (Stripe) or implement full webhook verification endpoint.

### Enable Detailed Logging

Add to `appsettings.json`:

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Business.Services.PaymentGateways": "Debug"
	}
  }
}
```

---

## 📊 Database Schema

### TbPaymentTransaction

Existing table, now used with real gateway data:

```sql
CREATE TABLE [dbo].[TbPaymentTransaction] (
	[Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	[ShipmentId] UNIQUEIDENTIFIER NOT NULL,
	[PaymentMethodId] UNIQUEIDENTIFIER NOT NULL,
	[ShippingRate] DECIMAL(18,2) NOT NULL,
	[CommissionPercentage] FLOAT NULL,
	[CommissionAmount] DECIMAL(18,2) NULL,
	[TotalAmount] DECIMAL(18,2) NOT NULL,
	[TransactionReference] NVARCHAR(255) NULL,  -- Now contains real gateway IDs
	[TransactionStatus] INT NOT NULL,
	[ProcessedDate] DATETIME2 NULL,
	[ErrorMessage] NVARCHAR(MAX) NULL,
	[Notes] NVARCHAR(MAX) NULL,
	[CreatedBy] UNIQUEIDENTIFIER NOT NULL,
	[CreatedDate] DATETIME2 NOT NULL,
	[UpdatedBy] UNIQUEIDENTIFIER NULL,
	[UpdatedDate] DATETIME2 NULL,
	[CurrentState] INT NOT NULL
);
```

Key changes in usage:
- `TransactionReference`: Now stores real Stripe charge ID (e.g., `pi_xxx`) or PayPal order ID
- `ErrorMessage`: Now contains real error messages from payment gateways
- `Notes`: Contains gateway name and processing details

---

## 🔒 Security Best Practices

### 1. Protect API Keys
- **Never commit secrets to Git**
- Use Azure Key Vault or environment variables in production
- Rotate keys regularly

### 2. Validate Webhooks
- Always validate webhook signatures
- Implement idempotency for webhook handlers
- Log all webhook events

### 3. PCI Compliance
- **Never store full card numbers**
- Use Stripe Elements or PayPal buttons for card input
- Let the payment gateway handle sensitive data

### 4. Error Handling
- Don't expose detailed error messages to users
- Log errors securely
- Use generic messages: "Payment failed. Please try again."

---

## 📞 Support Resources

### Stripe
- Documentation: https://stripe.com/docs
- API Reference: https://stripe.com/docs/api
- Support: https://support.stripe.com

### PayPal
- Documentation: https://developer.paypal.com/docs/
- API Reference: https://developer.paypal.com/api/rest/
- Support: https://developer.paypal.com/support/

---

## ✅ Implementation Checklist

Before going live:

- [ ] Replace test credentials with live credentials
- [ ] Set `Environment` to `Live` (Stripe) / `Live` (PayPal)
- [ ] Test payment flow end-to-end
- [ ] Test refund flow
- [ ] Implement webhook handlers
- [ ] Add proper error logging
- [ ] Review PCI compliance requirements
- [ ] Set up monitoring and alerts
- [ ] Document any custom business logic
- [ ] Train support team on payment troubleshooting

---

## 🎯 Key Takeaways

1. **Abstraction is powerful**: Adding a new payment gateway only requires implementing `IPaymentGateway`
2. **Factory pattern works**: Gateway selection is automatic and transparent
3. **Configuration-driven**: Easy to switch gateways or update credentials
4. **Production-ready**: Real payment processing, not simulation
5. **Extensible**: Ready for webhooks, subscriptions, and more

---

**Questions?** Check the full implementation summary: `Payment_Gateway_Implementation_Summary.md`
