# Payment Controllers - Visual Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         FRONTEND (Razor Pages / JS)                      │
└──────────┬──────────────┬──────────────┬──────────────┬─────────────────┘
		   │              │              │              │
		   │              │              │              │
	┌──────▼──────┐ ┌────▼─────┐ ┌──────▼──────┐ ┌────▼──────┐
	│  Payment    │ │ Payment  │ │  Payment    │ │  Webhook  │
	│ Controller  │ │ Methods  │ │Transactions │ │ Endpoint  │
	│             │ │Controller│ │ Controller  │ │ (External)│
	└──────┬──────┘ └────┬─────┘ └──────┬──────┘ └────┬──────┘
		   │              │              │              │
		   │              │              │              │
	┌──────▼──────────────▼──────────────▼──────────────▼─────────┐
	│              PaymentTransactionService                       │
	│         (Orchestrates payment processing logic)              │
	└──────┬──────────────┬──────────────┬─────────────────────────┘
		   │              │              │
	┌──────▼──────┐ ┌────▼─────┐ ┌──────▼──────┐
	│   Stripe    │ │  PayPal  │ │  Database   │
	│   Gateway   │ │  Gateway │ │ Repository  │
	└─────────────┘ └──────────┘ └─────────────┘
```

---

## 🎯 Request Flow Examples

### 1. Create PayPal Payment
```
User clicks "Pay with PayPal"
	↓
Frontend → POST /api/Payment/CreateOrder
	↓
PaymentController.CreateOrder()
	↓
PaymentTransactionService.CreateOrderAsync()
	↓
PayPalPaymentGateway.ProcessPayment()
	↓
PayPal API → Returns Order ID
	↓
Frontend shows PayPal popup
	↓
User approves payment
	↓
Frontend → POST /api/Payment/CaptureOrder
	↓
PaymentController.CaptureOrder()
	↓
PaymentTransactionService.CaptureOrderAsync()
	↓
PayPalPaymentGateway.CaptureOrder()
	↓
Database updated → Shipment marked paid ✅
```

### 2. Load Available Payment Methods
```
User visits checkout page
	↓
Frontend → GET /api/PaymentMethods
	↓
PaymentMethodsController.Get()
	↓
PaymentMethodsService.GetActivePaymentMethods()
	↓
Database → Returns [Credit Card, PayPal]
	↓
Frontend displays payment options ✅
```

### 3. View Transaction History
```
Admin clicks "View All Transactions"
	↓
Frontend → GET /api/PaymentTransactions/date-range?from=2025-01-01&to=2025-01-31
	↓
PaymentTransactionsController.GetByDateRange()
	↓
PaymentTransactionService.GetTransactionsByDateRange()
	↓
Database → Returns transactions
	↓
Frontend displays report ✅
```

### 4. Webhook Reconciliation (Background Process)
```
User completes payment on Stripe
	↓
Stripe → POST /api/PaymentWebhooks/stripe
	↓
PaymentWebhooksController.Stripe()
	↓
Validates signature ✅
	↓
Checks if already processed (idempotency) ✅
	↓
PaymentTransactionService.ReconcileTransactionFromWebhook()
	↓
Database updated → Shipment marked paid ✅
	↓
Returns 200 OK to Stripe
```

---

## 📊 Controller Responsibility Matrix

| Controller | Processing | Configuration | Reporting | Async Events |
|------------|-----------|---------------|-----------|--------------|
| **PaymentController** | ✅ Primary | ❌ | ❌ | ❌ |
| **PaymentMethodsController** | ❌ | ✅ Primary | ❌ | ❌ |
| **PaymentTransactionsController** | ✅ Delegates | ❌ | ✅ Primary | ❌ |
| **PaymentWebhooksController** | ❌ | ❌ | ❌ | ✅ Primary |

---

## 🔒 Security & Authentication

```
┌─────────────────────────────────────────────────────────────┐
│ PaymentController              [Authorize]                   │
│ PaymentMethodsController       [Authorize]                   │
│ PaymentTransactionsController  [Authorize]                   │
│ PaymentWebhooksController      [AllowAnonymous] + Signature  │
└─────────────────────────────────────────────────────────────┘
```

**Why?**
- First 3 require **user authentication** (JWT token)
- Webhooks are **server-to-server** (Stripe/PayPal → Your API)
- Webhooks use **signature validation** instead of auth tokens

---

## 🎓 Learning Path

### **Week 1: Basic CRUD**
Focus: `PaymentMethodsController`
- Implement GET all methods
- Add filtering, pagination
- Calculate commission logic

### **Week 2: Payment Processing**
Focus: `PaymentController`
- Integrate Stripe test mode
- Implement create → capture flow
- Handle payment failures

### **Week 3: Async Events**
Focus: `PaymentWebhooksController`
- Set up Stripe webhook endpoint
- Test with Stripe CLI
- Implement idempotency

### **Week 4: Reporting**
Focus: `PaymentTransactionsController`
- Query transactions by date
- Export to CSV/Excel
- Build admin dashboard

---

## 🛠️ Tools for Testing

### **Stripe CLI** (Webhook Testing)
```bash
# Install Stripe CLI
stripe login

# Forward webhooks to local server
stripe listen --forward-to https://localhost:7228/api/PaymentWebhooks/stripe

# Trigger test event
stripe trigger payment_intent.succeeded
```

### **Postman** (API Testing)
```
Collection: MyShipping Payment APIs
├── Payment Processing
│   ├── POST Create PayPal Order
│   ├── POST Capture PayPal Order
│   ├── POST Create Stripe Intent
│   └── POST Capture Stripe Payment
├── Payment Methods
│   ├── GET All Methods
│   ├── GET Method by ID
│   └── POST Calculate Total
├── Transactions
│   ├── GET Transaction by ID
│   └── GET Transactions by Date
└── Webhooks
	├── POST Stripe Webhook (Mock)
	└── POST PayPal Webhook (Mock)
```

---

## 📝 Quick Reference

### **When to use each controller?**

| Scenario | Controller | Endpoint |
|----------|-----------|----------|
| User wants to pay for shipment | PaymentController | `POST /api/Payment/CreateOrder` |
| Show available payment options | PaymentMethodsController | `GET /api/PaymentMethods` |
| Admin views payment history | PaymentTransactionsController | `GET /api/PaymentTransactions/date-range` |
| Stripe sends payment confirmation | PaymentWebhooksController | `POST /api/PaymentWebhooks/stripe` |
| Calculate total with commission | PaymentMethodsController | `POST /api/PaymentMethods/calculate-total` |
| Process refund | PaymentController | `POST /api/Payment/Refund` |

---

## 🎯 Summary

**4 Controllers = 4 Concerns**
1. **Processing** payments (PaymentController)
2. **Configuring** payment options (PaymentMethodsController)
3. **Querying** transaction history (PaymentTransactionsController)
4. **Receiving** async events (PaymentWebhooksController)

**Benefits**: Clean separation, easier testing, scalable architecture  
**Trade-off**: More files, but clearer purpose  
**Recommendation**: Keep all 4 for learning production patterns!
