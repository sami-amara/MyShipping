# PayPal Integration Testing Guide

## ✅ Payment Successfully Processed!

Your first PayPal payment just succeeded! Now let's verify everything and test webhooks.

---

## 📋 Part 1: Verify Payment in PayPal Sandbox

### Steps:

1. **Open PayPal Developer Dashboard**
   - URL: https://developer.paypal.com/dashboard/
   - Log in with your PayPal account

2. **Access Sandbox Test Accounts**
   - Click **"Testing Tools"** → **"Sandbox Accounts"** (left menu)
   - You'll see two types:
	 - **Personal** (buyer account - test customer)
	 - **Business** (merchant account - receives payments)

3. **Login to Business Sandbox Account**
   - Find your **Business** sandbox account in the list
   - Click the **"..."** menu (three dots) next to it
   - Select **"View/Edit Account"**
   - Click **"Login to Sandbox"** button
   - A new tab opens with sandbox PayPal interface

4. **View Transaction**
   - In the sandbox PayPal interface, click **"Activity"** or **"Transactions"**
   - You should see your test payment! 💰
   - Click on it to see details (amount, date, payer info, transaction ID)

5. **Match Transaction ID**
   - Copy the PayPal transaction ID
   - Check your `PaymentTransactions` table in database
   - The `ExternalTransactionId` should match PayPal's ID ✅

---

## 🔔 Part 2: Test Webhook Delivery

### Prerequisites

✅ Payment gateway working (just confirmed!)  
⚠️ ngrok must be running (currently not running)  
⚠️ PayPal webhook URL must match current ngrok URL  

---

### Option A: Quick Test with PayPal Simulator

**No ngrok needed - uses PayPal's built-in test sender**

1. **Go to PayPal Webhooks**
   - https://developer.paypal.com/dashboard/
   - **Apps & Credentials** → **Sandbox** tab → **Webhooks**

2. **Find Your Webhook**
   - Should see webhook with your ngrok URL (even if ngrok is down, you can still simulate)

3. **Simulate Event**
   - Click on your webhook
   - Click **"Simulate Event"** or **"Send Test Event"** button
   - Select event type: **"Payment capture completed"** (`PAYMENT.CAPTURE.COMPLETED`)
   - Click **"Send"**

4. **Important Note:**
   - ⚠️ Simulation will FAIL if ngrok is not running
   - ✅ But you'll see the request in PayPal's delivery history
   - Start ngrok first for simulation to succeed

---

### Option B: Real Webhook Test (Recommended)

**Tests the complete end-to-end flow**

#### Step 1: Start ngrok ⚡

Open PowerShell (keep it open!):

```powershell
cd E:\
.\ngrok http 5228
```

**Output will show:**
```
Forwarding  https://some-random-name.ngrok-free.dev -> http://localhost:5228
```

📝 **Copy the HTTPS URL** (changes every time you restart ngrok!)

---

#### Step 2: Update PayPal Webhook URL 🔗

1. Go to PayPal Developer Dashboard → Webhooks
2. Click on your webhook (or create new if missing)
3. **Update Webhook URL:**
   ```
   https://[YOUR-NEW-NGROK-URL]/api/PaymentWebhooks/paypal
   ```
   Example:
   ```
   https://shortcut-crushing-conjoined.ngrok-free.dev/api/PaymentWebhooks/paypal
   ```
4. **Select Event Types** (if creating new):
   - ✅ `PAYMENT.CAPTURE.COMPLETED`
   - ✅ `PAYMENT.CAPTURE.REFUNDED`
   - ✅ `PAYMENT.CAPTURE.DENIED`
5. **Save**
6. **Copy Webhook ID** and update `appsettings.json`:
   ```json
   "PayPal": {
	 "WebhookId": "YOUR-ACTUAL-WEBHOOK-ID-HERE"
   }
   ```

---

#### Step 3: Make Test Payment 💳

1. **Create a new shipment** in your application
2. **Select PayPal** as payment method
3. **Submit**
4. Payment should succeed ✅

---

#### Step 4: Monitor Webhook Delivery 👀

**Option 1: ngrok Web UI (Best for Debugging)**

1. Open browser: **http://localhost:4040**
2. You'll see all HTTP requests in real-time
3. Look for POST request to `/api/PaymentWebhooks/paypal`
4. Click on it to see:
   - Request headers (including PayPal signature)
   - Request body (webhook event JSON)
   - Response status (200 OK if processed successfully)

**Example of what you'll see:**
```
POST /api/PaymentWebhooks/paypal
Status: 200 OK
Headers:
  PayPal-Transmission-Id: xxxxx
  PayPal-Transmission-Time: 2024-01-15T10:30:00Z
  PayPal-Transmission-Sig: xxxxx
Body:
  {
	"event_type": "PAYMENT.CAPTURE.COMPLETED",
	"resource": { ... }
  }
```

---

**Option 2: Check Database**

Run this query in SQL Server Management Studio or VS SQL Server Object Explorer:

```sql
-- Check received webhooks
SELECT TOP 10
	EventId,
	EventType,
	Provider,
	ReceivedAt,
	ProcessedAt,
	IsProcessed,
	TransactionId
FROM WebhookEvents
WHERE Provider = 'PayPal'
ORDER BY ReceivedAt DESC;
```

**Expected Result:**
```
EventId                  | EventType                    | ReceivedAt           | IsProcessed
WH-xxx-xxx              | PAYMENT.CAPTURE.COMPLETED    | 2024-01-15 10:30:00  | 1
```

---

**Option 3: Check WebApi Logs**

Look in your console output or `Logs/security-*.txt` for:

```
[Information] PayPal webhook received: PAYMENT.CAPTURE.COMPLETED
[Information] Webhook signature validated successfully
[Information] Transaction reconciled from webhook: {transactionId}
```

---

#### Step 5: Verify Reconciliation ✅

Check that the webhook updated the payment transaction:

```sql
-- Check if webhook reconciled the transaction
SELECT 
	wh.EventId,
	wh.EventType,
	wh.ReceivedAt,
	wh.IsProcessed,
	pt.TransactionId,
	pt.Status,
	pt.Amount,
	pt.ExternalTransactionId
FROM WebhookEvents wh
INNER JOIN PaymentTransactions pt ON wh.TransactionId = pt.Id
WHERE wh.Provider = 'PayPal'
ORDER BY wh.ReceivedAt DESC;
```

**Expected:** Webhook event linked to payment transaction ✅

---

## 🧪 Part 3: Test Duplicate Webhook Prevention

PayPal sometimes sends the same webhook multiple times. Your code should handle this gracefully.

### Test Steps:

1. **In PayPal Webhooks Dashboard:**
   - Find a delivered webhook event
   - Click **"Resend"** button
   - PayPal sends the same event again

2. **Expected Behavior:**
   - First webhook: Processed ✅
   - Duplicate webhook: Skipped (already processed) ⏭️
   - Both show in `WebhookEvents` table
   - Transaction NOT reconciled twice

3. **Check Logs:**
   ```
   [Information] Webhook event already processed, skipping: {eventId}
   ```

4. **Database Check:**
   ```sql
   -- Both webhooks should have same EventId but only one processed the transaction
   SELECT EventId, COUNT(*) as Count
   FROM WebhookEvents
   WHERE Provider = 'PayPal'
   GROUP BY EventId
   HAVING COUNT(*) > 1;
   ```

---

## 🔍 Part 4: Troubleshooting

### Issue: Webhook not received in ngrok

**Possible Causes:**
1. ❌ ngrok not running → Start it!
2. ❌ PayPal webhook URL doesn't match ngrok URL → Update in PayPal dashboard
3. ❌ Firewall blocking ngrok → Check Windows Firewall
4. ❌ ngrok authtoken not set → Run `.\ngrok config add-authtoken YOUR-TOKEN`

**How to Check:**
- ngrok web UI (localhost:4040) shows incoming requests
- If empty, webhook never reached ngrok
- If you see 502 Bad Gateway, ngrok can't reach your WebApi (check if WebApi is running)

---

### Issue: Webhook received but validation fails

**Possible Causes:**
1. ❌ Wrong WebhookId in appsettings.json
2. ❌ Webhook signature mismatch

**How to Fix:**
- Copy correct Webhook ID from PayPal dashboard
- Update `appsettings.json` → restart WebApi
- Check logs for validation error details

---

### Issue: Webhook processed but transaction not reconciled

**Possible Causes:**
1. ❌ ExternalTransactionId doesn't match
2. ❌ Transaction not in database yet

**How to Check:**
```sql
-- Find orphaned webhooks (no matching transaction)
SELECT wh.*
FROM WebhookEvents wh
LEFT JOIN PaymentTransactions pt ON wh.TransactionId = pt.Id
WHERE wh.Provider = 'PayPal' AND pt.Id IS NULL;
```

---

## ✅ Success Criteria Checklist

- [ ] Payment visible in PayPal Sandbox dashboard
- [ ] PaymentTransaction record in database with ExternalTransactionId
- [ ] ngrok running and forwarding to localhost:5228
- [ ] PayPal webhook URL updated with current ngrok URL
- [ ] Webhook event received (visible in ngrok UI)
- [ ] WebhookEvent record created in database
- [ ] Webhook signature validated successfully
- [ ] Transaction reconciled (webhook linked to transaction)
- [ ] Duplicate webhook handled (skipped, not reprocessed)
- [ ] Logs show successful webhook processing

---

## 📊 Useful Database Queries

Save this file: `Database_Queries/Check_PayPal_Webhooks.sql`

```sql
-- Recent PayPal webhooks
SELECT TOP 10
	EventId,
	EventType,
	Provider,
	ReceivedAt,
	ProcessedAt,
	IsProcessed
FROM WebhookEvents
WHERE Provider = 'PayPal'
ORDER BY ReceivedAt DESC;

-- Recent PayPal transactions
SELECT TOP 10
	Id,
	Amount,
	Currency,
	Status,
	TransactionId,
	ExternalTransactionId,
	CreatedAt
FROM PaymentTransactions
ORDER BY CreatedAt DESC;

-- Webhooks with reconciled transactions
SELECT 
	wh.EventId,
	wh.EventType,
	wh.ReceivedAt,
	pt.TransactionId,
	pt.Status,
	pt.Amount,
	pt.ExternalTransactionId
FROM WebhookEvents wh
INNER JOIN PaymentTransactions pt ON wh.TransactionId = pt.Id
WHERE wh.Provider = 'PayPal'
ORDER BY wh.ReceivedAt DESC;

-- Duplicate webhook events (same EventId received multiple times)
SELECT EventId, COUNT(*) as DeliveryCount
FROM WebhookEvents
WHERE Provider = 'PayPal'
GROUP BY EventId
HAVING COUNT(*) > 1;
```

---

## 🎯 Next Steps

1. ✅ **Verify payment in PayPal dashboard** (Part 1)
2. 🔄 **Start ngrok** (Part 2, Step 1)
3. 🔗 **Update webhook URL** (Part 2, Step 2)
4. 💳 **Make another test payment** (Part 2, Step 3)
5. 👀 **Watch webhook arrive in ngrok UI** (Part 2, Step 4)
6. ✅ **Confirm reconciliation in database** (Part 2, Step 5)
7. 🧪 **Test duplicate prevention** (Part 3)

---

## 🎓 Understanding the Flow

```
User creates shipment
	↓
WebApi processes PayPal payment
	↓
Payment succeeds → saved to PaymentTransactions
	↓
PayPal sends webhook → ngrok → WebApi
	↓
WebApi validates signature
	↓
WebApi checks if already processed (idempotency)
	↓
WebApi reconciles transaction (updates status if needed)
	↓
WebApi marks webhook as processed
```

---

**Good luck with testing! Let me know what you find in the PayPal dashboard and if you need help setting up ngrok!** 🚀
