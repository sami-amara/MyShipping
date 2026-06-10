# Payment Gateway Testing Guide

## 🧪 Testing Strategy Overview

This guide provides comprehensive testing instructions for the payment gateway integration, covering unit tests, integration tests, and manual testing scenarios.

---

## 📋 Test Environment Setup

### 1. Stripe Test Mode Configuration

Update `UI/appsettings.json`:

```json
"Stripe": {
  "Enabled": true,
  "PublishableKey": "pk_test_51XXXXX",
  "SecretKey": "sk_test_51XXXXX",
  "WebhookSecret": "whsec_XXXXX",
  "Environment": "Test",
  "ApiVersion": null
}
```

Get test keys from: https://dashboard.stripe.com/test/apikeys

### 2. PayPal Sandbox Configuration

Update `UI/appsettings.json`:

```json
"PayPal": {
  "Enabled": true,
  "ClientId": "YOUR_SANDBOX_CLIENT_ID",
  "ClientSecret": "YOUR_SANDBOX_CLIENT_SECRET",
  "WebhookId": "YOUR_WEBHOOK_ID",
  "Environment": "Sandbox",
  "BaseUrl": null
}
```

Get sandbox credentials from: https://developer.paypal.com/dashboard/applications/sandbox

---

## 🎯 Test Scenarios

### Scenario 1: Successful Stripe Payment

**Setup:**
1. Create a shipment in the system
2. Select "Credit Card" as payment method
3. Use test card: `4242 4242 4242 4242`

**Expected Result:**
- Payment processes successfully
- Transaction status: `Completed`
- TransactionReference contains Stripe payment intent ID (starts with `pi_`)
- ProcessedDate is set
- Notes mention "Stripe"

**Verification SQL:**
```sql
SELECT TOP 1 * FROM TbPaymentTransaction 
ORDER BY CreatedDate DESC
```

### Scenario 2: Failed Stripe Payment

**Setup:**
1. Create a shipment
2. Use test card: `4000 0000 0000 9995` (always declined)

**Expected Result:**
- Payment fails gracefully
- Transaction status: `Failed`
- ErrorMessage contains decline reason
- TransactionReference may be empty
- ProcessedDate is null

### Scenario 3: Stripe 3D Secure Required

**Setup:**
1. Use test card: `4000 0027 6000 3184`

**Expected Result:**
- Payment status: `RequiresAction`
- Additional authentication needed
- Can be handled in future webhook implementation

### Scenario 4: Successful PayPal Payment

**Setup:**
1. Create a shipment
2. Select "PayPal" as payment method
3. Complete PayPal sandbox login flow

**Expected Result:**
- PayPal order created
- Transaction status: `Completed` (if captured) or `Pending` (if not)
- TransactionReference contains PayPal order ID
- Notes mention "PayPal"

### Scenario 5: Full Refund (Stripe)

**Setup:**
1. Complete a successful Stripe payment (Scenario 1)
2. Call refund endpoint with full amount

**Expected Result:**
- Refund processes successfully
- Transaction status updates to `Refunded`
- Notes include refund ID and timestamp
- Refund visible in Stripe dashboard

### Scenario 6: Full Refund (PayPal)

**Setup:**
1. Complete a successful PayPal payment (Scenario 4)
2. Call refund endpoint with full amount

**Expected Result:**
- Refund processes successfully
- Transaction status updates to `Refunded`
- Notes include PayPal refund ID
- Refund visible in PayPal dashboard

### Scenario 7: Partial Refund

**Setup:**
1. Complete a payment for $100
2. Refund $30

**Expected Result:**
- Partial refund succeeds
- Transaction status: `PartiallyRefunded` (if implemented)
- Can refund remaining amount later

### Scenario 8: Gateway Selection by Payment Method Name

**Test Cases:**

| Input Payment Method | Expected Gateway |
|----------------------|------------------|
| "CreditCard"         | StripePaymentGateway |
| "Credit Card"        | StripePaymentGateway |
| "Visa"               | StripePaymentGateway |
| "MasterCard"         | StripePaymentGateway |
| "PayPal"             | PayPalPaymentGateway |
| "Unknown"            | StripePaymentGateway (default) |

**Verification:**
Check logs or add breakpoint in `PaymentGatewayFactory.GetGateway()`

### Scenario 9: Gateway Selection by Payment Method ID

**Setup:**
1. Get payment method ID from database
2. Call `GetGatewayByIdAsync(paymentMethodId)`

**Expected Result:**
- Correct gateway selected based on method name in database
- Factory queries database and routes correctly

### Scenario 10: Error Handling - Invalid API Key

**Setup:**
1. Set invalid Stripe SecretKey in config
2. Attempt payment

**Expected Result:**
- Payment fails gracefully
- Error logged
- User sees generic error message
- No sensitive data exposed

---

## 🔬 Unit Test Examples

### Test 1: Payment Request Mapping

```csharp
[Fact]
public void PaymentRequest_ShouldMapCorrectly()
{
	// Arrange
	var request = new PaymentRequest
	{
		Amount = 100.00m,
		Currency = "USD",
		Description = "Test payment",
		Customer = new CustomerInfo
		{
			UserId = Guid.NewGuid().ToString(),
			Email = "test@example.com"
		}
	};

	// Assert
	Assert.Equal(100.00m, request.Amount);
	Assert.Equal("USD", request.Currency);
	Assert.NotNull(request.Customer);
}
```

### Test 2: Payment Status Mapping

```csharp
[Theory]
[InlineData("succeeded", PaymentStatus.Completed)]
[InlineData("processing", PaymentStatus.Pending)]
[InlineData("requires_action", PaymentStatus.RequiresAction)]
[InlineData("canceled", PaymentStatus.Canceled)]
public void MapStripeStatus_ShouldReturnCorrectStatus(string stripeStatus, PaymentStatus expected)
{
	// Act
	var result = MapStripeStatusHelper(stripeStatus);

	// Assert
	Assert.Equal(expected, result);
}
```

### Test 3: Gateway Factory Selection

```csharp
[Theory]
[InlineData("CreditCard", typeof(StripePaymentGateway))]
[InlineData("PayPal", typeof(PayPalPaymentGateway))]
[InlineData("Unknown", typeof(StripePaymentGateway))]
public void GetGateway_ShouldReturnCorrectGateway(string methodName, Type expectedType)
{
	// Arrange
	var factory = new PaymentGatewayFactory(_serviceProvider, _paymentMethodRepo);

	// Act
	var gateway = factory.GetGateway(methodName);

	// Assert
	Assert.IsType(expectedType, gateway);
}
```

### Test 4: Refund Request Validation

```csharp
[Fact]
public void RefundRequest_ShouldValidateAmount()
{
	// Arrange
	var request = new RefundRequest
	{
		TransactionId = "pi_test123",
		Amount = -10.00m, // Invalid
		Currency = "USD"
	};

	// Act & Assert
	Assert.Throws<ArgumentException>(() => ValidateRefundRequest(request));
}
```

---

## 🔗 Integration Test Examples

### Test 1: End-to-End Stripe Payment

```csharp
[Fact]
public async Task ProcessPayment_WithStripe_ShouldSucceed()
{
	// Arrange
	var shipmentId = Guid.NewGuid();
	var paymentMethodId = GetCreditCardPaymentMethodId();
	var shippingRate = 50.00m;

	// Act
	var result = await _paymentService.ProcessPayment(
		shipmentId, 
		paymentMethodId, 
		shippingRate
	);

	// Assert
	Assert.NotNull(result);
	Assert.Equal("Completed", result.TransactionStatusName);
	Assert.NotNull(result.TransactionReference);
	Assert.True(result.TransactionReference.StartsWith("pi_"));
}
```

### Test 2: End-to-End Refund

```csharp
[Fact]
public async Task ProcessRefund_WithValidTransaction_ShouldSucceed()
{
	// Arrange - First create a payment
	var payment = await CreateTestPayment();

	// Act - Then refund it
	var refund = await _paymentService.SimulateRefund(
		payment.Id, 
		"Test refund"
	);

	// Assert
	Assert.NotNull(refund);
	Assert.Equal("Refunded", refund.TransactionStatusName);
	Assert.Contains("REFUND", refund.Notes);
}
```

---

## 📊 Manual Testing Checklist

### Payment Processing
- [ ] Credit card payment via Stripe succeeds
- [ ] Credit card payment with declined card fails gracefully
- [ ] PayPal payment flow works end-to-end
- [ ] Commission calculation is correct
- [ ] Transaction reference is stored correctly
- [ ] Payment method name is displayed correctly

### Refund Processing
- [ ] Full refund via Stripe works
- [ ] Full refund via PayPal works
- [ ] Partial refund works (if implemented)
- [ ] Refund on already-refunded transaction fails
- [ ] Refund ID is captured and stored

### Gateway Selection
- [ ] "CreditCard" method routes to Stripe
- [ ] "PayPal" method routes to PayPal
- [ ] Unknown method defaults to Stripe
- [ ] Database ID lookup works correctly

### Error Handling
- [ ] Invalid API key returns user-friendly error
- [ ] Network timeout is handled gracefully
- [ ] Duplicate payment attempts are prevented
- [ ] Invalid amount (negative, zero) is rejected

### Configuration
- [ ] Test mode works with test credentials
- [ ] Switching default gateway works
- [ ] Disabling a gateway prevents its use
- [ ] Currency setting is respected

### Database
- [ ] Transaction records are created correctly
- [ ] Status updates work
- [ ] Notes field captures gateway details
- [ ] Timestamps are accurate (UTC)

---

## 🧪 Stripe Test Cards

### Successful Payments

| Card Number | Brand | CVC | Date | Result |
|-------------|-------|-----|------|--------|
| 4242 4242 4242 4242 | Visa | Any 3 digits | Any future date | Success |
| 5555 5555 5555 4444 | Mastercard | Any 3 digits | Any future date | Success |
| 3782 822463 10005 | American Express | Any 4 digits | Any future date | Success |

### Failed Payments

| Card Number | Reason |
|-------------|--------|
| 4000 0000 0000 9995 | Always declined |
| 4000 0000 0000 9987 | Declined (insufficient funds) |
| 4000 0000 0000 9979 | Declined (stolen card) |

### Special Cases

| Card Number | Behavior |
|-------------|----------|
| 4000 0027 6000 3184 | Requires 3D Secure authentication |
| 4000 0000 0000 3220 | Requires 3D Secure 2 |

---

## 💳 PayPal Test Accounts

### Creating Test Accounts

1. Go to https://developer.paypal.com/dashboard/
2. Navigate to **Sandbox** > **Accounts**
3. Click **Create Account**
4. Create:
   - **Personal Account** (buyer)
   - **Business Account** (seller)

### Test Account Details

Default sandbox accounts typically have:
- **Email:** something@personal.example.com
- **Password:** (shown in dashboard)
- **Balance:** $9,999.99 USD

---

## 🔍 Debugging Tips

### Enable Detailed Logging

Add to `appsettings.json`:

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Business.Services.PaymentGateways.StripePaymentGateway": "Debug",
	  "Business.Services.PaymentGateways.PayPalPaymentGateway": "Debug",
	  "Business.Services.PaymentTransactionService": "Debug"
	}
  }
}
```

### SQL Queries for Testing

**Check latest transactions:**
```sql
SELECT TOP 10 
	Id,
	TransactionReference,
	TransactionStatus,
	TotalAmount,
	ErrorMessage,
	Notes,
	CreatedDate
FROM TbPaymentTransaction
ORDER BY CreatedDate DESC
```

**Check transactions by status:**
```sql
SELECT 
	COUNT(*) as Count,
	TransactionStatus
FROM TbPaymentTransaction
GROUP BY TransactionStatus
```

**Find failed payments:**
```sql
SELECT * FROM TbPaymentTransaction
WHERE TransactionStatus = 2 -- Failed
ORDER BY CreatedDate DESC
```

### Stripe Dashboard

Monitor test payments in real-time:
- Payments: https://dashboard.stripe.com/test/payments
- Logs: https://dashboard.stripe.com/test/logs

### PayPal Sandbox Dashboard

Monitor test transactions:
- Activity: https://sandbox.paypal.com (login with test account)
- Developer logs: https://developer.paypal.com/dashboard/

---

## 🚨 Common Issues & Solutions

### Issue 1: "No such payment_intent"
**Cause:** Transaction ID doesn't exist in Stripe  
**Solution:** Verify TransactionReference in database matches Stripe payment intent ID

### Issue 2: "Authentication failed"
**Cause:** Invalid API credentials  
**Solution:** Double-check `SecretKey` in appsettings.json

### Issue 3: "Amount must be at least $0.50"
**Cause:** Stripe minimum amount requirement  
**Solution:** Ensure payment amount ≥ $0.50 USD

### Issue 4: PayPal "AUTHENTICATION_FAILURE"
**Cause:** Invalid ClientId or ClientSecret  
**Solution:** Regenerate credentials in PayPal dashboard

### Issue 5: "Payment already refunded"
**Cause:** Attempting to refund twice  
**Solution:** Check transaction status before refund

---

## ✅ Test Completion Checklist

### Before Production Deployment

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Manual testing completed for all scenarios
- [ ] Error handling verified for all failure modes
- [ ] Logging is comprehensive but not excessive
- [ ] Test data cleaned up
- [ ] Production credentials obtained and secured
- [ ] Webhook endpoints tested (if implemented)
- [ ] Performance tested under load
- [ ] Security audit completed
- [ ] Documentation reviewed and updated

---

## 📈 Performance Testing

### Load Test Scenarios

1. **Concurrent Payments**
   - 10 simultaneous payments
   - 50 simultaneous payments
   - 100 simultaneous payments

2. **Response Time**
   - Stripe payment should complete in < 3 seconds
   - PayPal order creation should complete in < 5 seconds
   - Refunds should complete in < 2 seconds

3. **Timeout Handling**
   - Simulate network delays
   - Verify graceful degradation

### Monitoring Metrics

- Payment success rate (target: > 95%)
- Average response time (target: < 2s)
- Error rate (target: < 5%)
- Refund success rate (target: > 98%)

---

## 🎯 Acceptance Criteria

For each payment gateway integration:

✅ **Functional**
- Payment processing works end-to-end
- Refunds work correctly
- Status updates are accurate
- Error messages are clear

✅ **Non-Functional**
- Response time < 3 seconds for 95th percentile
- No sensitive data logged
- API keys properly secured
- Gateway selection is transparent

✅ **Operational**
- Monitoring and alerts configured
- Support team trained
- Documentation complete
- Rollback plan ready

---

## 📝 Test Report Template

```markdown
# Payment Gateway Test Report

**Date:** YYYY-MM-DD  
**Tester:** Name  
**Environment:** Test/Staging/Production

## Test Summary
- Total Tests: XX
- Passed: XX
- Failed: XX
- Skipped: XX

## Stripe Tests
- [ ] Successful payment
- [ ] Declined payment
- [ ] 3D Secure payment
- [ ] Refund
- [ ] Invalid credentials

**Issues Found:** None / [List issues]

## PayPal Tests
- [ ] Successful order creation
- [ ] Payment capture
- [ ] Refund
- [ ] Sandbox authentication

**Issues Found:** None / [List issues]

## Recommendations
[Any recommendations for improvements]

## Sign-off
Tested by: ___________  
Approved by: ___________
```

---

## 🎉 Ready for Production?

Use this checklist to determine readiness:

- [ ] All test scenarios pass
- [ ] Error handling verified
- [ ] Security review completed
- [ ] Performance acceptable
- [ ] Monitoring configured
- [ ] Documentation complete
- [ ] Team trained
- [ ] Production credentials obtained
- [ ] Rollback plan documented
- [ ] Customer support ready

**When all boxes are checked, you're ready to go live!** 🚀
