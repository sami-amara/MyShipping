# 🧪 Stripe Payment Gateway Test Instructions

## ✅ You're Ready to Test!

Your Stripe integration is configured and ready. Here's how to test it:

---

## 🚀 How to Test

### Step 1: Access the Test Page

Once your application is running, navigate to:

```
https://localhost:XXXX/PaymentTest
```

Replace `XXXX` with your application's port number.

---

### Step 2: Test a Stripe Payment

1. **Enter Amount:** The default is $10.00 (minimum $0.50)
2. **Click "Test Stripe Payment"**
3. **Wait for Response:** You'll see a success message with:
   - ✅ Transaction ID (starts with `pi_`)
   - Payment status
   - Amount charged
   - Gateway name (Stripe)

**Example Success Response:**
```json
{
  "success": true,
  "gateway": "Stripe",
  "transactionId": "pi_3XXXXXXXXXXXXX",
  "status": "Completed",
  "amount": 10.00,
  "currency": "USD",
  "processedAt": "2024-XX-XX..."
}
```

---

### Step 3: Verify in Stripe Dashboard

1. Open your Stripe dashboard: https://dashboard.stripe.com/test/payments
2. You should see your test payment listed
3. Click on it to see full details

---

### Step 4: Test a Refund

1. **Copy the Transaction ID** from the successful payment above
2. **Paste it** into the "Transaction ID" field in the Refund section
3. **Leave "Refund Amount" empty** for a full refund (or enter an amount for partial)
4. **Click "Test Stripe Refund"**
5. **Check the Response:** You'll see refund confirmation

**Example Refund Response:**
```json
{
  "success": true,
  "refundId": "re_XXXXXXXXXXXXX",
  "status": "Completed",
  "amountRefunded": 10.00
}
```

---

## 🃏 Stripe Test Cards

The test page shows these cards, but here's a quick reference:

| Card Number | Result | Use Case |
|-------------|--------|----------|
| `4242 4242 4242 4242` | ✅ Success | Normal successful payment |
| `4000 0000 0000 9995` | ❌ Declined | Test failure handling |
| `4000 0027 6000 3184` | ⚠️ 3D Secure | Test authentication flow |

**Details:**
- Use **any future expiry date** (e.g., 12/25)
- Use **any 3-digit CVC** (e.g., 123)
- Use **any ZIP code** (e.g., 12345)

---

## 🔍 What's Happening Behind the Scenes?

When you click "Test Stripe Payment":

1. **Gateway Selection:**
   ```csharp
   var gateway = _gatewayFactory.GetGateway("CreditCard");
   // Returns: StripePaymentGateway instance
   ```

2. **Payment Request:**
   ```csharp
   var request = new PaymentRequest {
	   Amount = 10.00m,
	   Currency = "USD",
	   Description = "Stripe Test Payment",
	   CaptureImmediately = true
   };
   ```

3. **Stripe API Call:**
   - Creates a Stripe Payment Intent
   - Uses your `sk_test_...` secret key
   - Automatically confirms and captures the payment

4. **Result:**
   - Returns transaction ID (Stripe's payment intent ID)
   - Payment status (Completed, Failed, etc.)
   - Timestamps and metadata

---

## 🐛 Troubleshooting

### Error: "Invalid API Key"
**Cause:** Your Stripe secret key is incorrect  
**Fix:** 
1. Go to https://dashboard.stripe.com/test/apikeys
2. Copy your secret key (`sk_test_...`)
3. Update `UI/appsettings.json`:
   ```json
   "Stripe": {
	 "SecretKey": "sk_test_YOUR_ACTUAL_KEY"
   }
   ```
4. Restart the application

### Error: "Amount must be at least $0.50 usd"
**Cause:** Stripe requires minimum $0.50  
**Fix:** Enter an amount ≥ $0.50

### Error: "No such payment_intent"
**Cause:** Transaction ID doesn't exist  
**Fix:** Make sure you're using the exact transaction ID from a successful payment

### Test Page Not Loading
**Cause:** Route not found  
**Fix:** 
- Make sure the URL is `/PaymentTest` (not `/PaymentTest/Index`)
- Clear browser cache
- Check application logs

---

## 💡 Advanced Testing

### Test Different Amounts

Try these scenarios:
- **Minimum:** $0.50
- **Standard:** $10.00
- **Large:** $500.00
- **Decimal:** $12.34

### Test Partial Refunds

1. Make a $20.00 payment
2. Refund $5.00
3. Check Stripe dashboard - you can refund the remaining $15.00 later

### Monitor API Calls

Enable detailed logging in `appsettings.json`:

```json
{
  "Logging": {
	"LogLevel": {
	  "Business.Services.PaymentGateways.StripePaymentGateway": "Debug"
	}
  }
}
```

---

## 📊 Stripe Dashboard Guide

### Where to Find Your Test Payments

1. **Dashboard Home:** https://dashboard.stripe.com/test
2. **Payments List:** https://dashboard.stripe.com/test/payments
3. **Refunds:** https://dashboard.stripe.com/test/payments?status=refunded
4. **Logs:** https://dashboard.stripe.com/test/logs (see all API calls)

### What You'll See

- **Payment Intent ID:** Matches your transaction ID
- **Amount:** $10.00 USD
- **Status:** Succeeded
- **Description:** "Stripe Test Payment"
- **Metadata:** TestId, Environment
- **Timeline:** All events (created, confirmed, succeeded)

---

## 📝 Next Steps After Testing

Once you verify Stripe works:

1. ✅ **Test Refunds:** Make sure refunds work end-to-end
2. ✅ **Check Database:** Verify transactions are saved in `TbPaymentTransaction`
3. ✅ **Integrate with Shipments:** Connect payment processing to your shipment workflow
4. ✅ **Add UI:** Create proper checkout page with Stripe Elements
5. ✅ **Implement Webhooks:** Handle async payment notifications
6. ✅ **Test in Production:** Get live API keys and test with real (small) amounts

---

## 🎯 Success Criteria

Your test is successful if:

- ✅ Payment completes without errors
- ✅ Transaction ID is returned (starts with `pi_`)
- ✅ Payment appears in Stripe dashboard
- ✅ Refund processes successfully
- ✅ Refund appears in Stripe dashboard
- ✅ Both payment and refund show correct amounts

---

## 🔐 Important Notes

### About the WebhookSecret

You asked about the `WebhookSecret` in `appsettings.json`:

```json
"WebhookSecret": "whsec_YOUR_STRIPE_WEBHOOK_SECRET"
```

**What it is:**
- Used to verify webhook signatures
- Ensures webhooks actually come from Stripe
- Not needed for direct API calls

**When you need it:**
- When implementing webhook endpoints
- For async payment notifications
- For subscription renewals

**For now:**
- ✅ You can leave it as a placeholder
- ✅ Direct payments work without it
- ⚠️ Don't delete it (needed for Phase 6)

**How to get it (when needed):**
1. Go to https://dashboard.stripe.com/test/webhooks
2. Click "Add endpoint"
3. Enter URL: `https://your-domain.com/api/webhooks/stripe`
4. Select events to listen for
5. Copy the "Signing secret" (starts with `whsec_`)

---

## 🎉 You're All Set!

Your Stripe integration is **production-ready** (for test mode). Once you verify everything works:

1. Get your **live API keys** from Stripe
2. Update `Environment` to `"Live"`
3. Update `SecretKey` and `PublishableKey` with live keys
4. Test with a real (small) payment
5. Go live! 🚀

---

**Questions or Issues?**  
Check the full documentation:
- `Payment_Gateway_Implementation_Summary.md`
- `Payment_Gateway_Quick_Reference.md`
- `Payment_Gateway_Testing_Guide.md`
