# Stripe ProviderName Missing - Root Cause & Fix

## 🐛 Problem Discovered During Debugging

**Symptom**: 
- Trying to refund a **Stripe** payment
- Debugger shows `providerName = "PayPal"` at line 607
- Gateway factory returns `PayPalPaymentGateway` instead of `StripePaymentGateway`
- PayPal API rejects the Stripe transaction ID with `RESOURCE_NOT_FOUND`

**Error**:
```json
{
  "field": "capture_id",
  "value": "pi_3TVvzP21f6FJ6DMw12YFjDkg",
  "description": "Specified resource ID does not exist"
}
```

---

## 🔍 Root Cause Analysis

### **Issue**: Missing `ProviderName` in Stripe Transactions

**Location**: `Business/Services/PaymentTransactionService.cs`

#### **PayPal Capture Flow** (✅ Correct)
```csharp
// Line 271-285: Create transaction DTO
var transactionDto = new PaymentTransactionDto
{
	ShipmentId = request.ShipmentId,
	PaymentMethodId = request.PaymentMethodId,
	// ... other fields
	// ❌ ProviderName NOT set in DTO
};

var savedTransaction = await AddAsync(transactionDto);

// ✅ GOOD: ProviderName set AFTER saving (Line 294-300)
var persistedTransaction = await _repository.GetById(savedTransaction.Id);
if (persistedTransaction != null)
{
	persistedTransaction.ProviderName = "PayPal";  // ← SET HERE
	persistedTransaction.UpdatedDate = DateTime.UtcNow;
	persistedTransaction.UpdatedBy = _userService.GetLoggedInUser();
	await _repository.Update(persistedTransaction);
}
```

#### **Stripe Capture Flow** (❌ Missing)
```csharp
// Line 402-416: Create transaction DTO
var transactionDto = new PaymentTransactionDto
{
	ShipmentId = request.ShipmentId,
	PaymentMethodId = stripePaymentMethodId.Value,
	// ... other fields
	// ❌ ProviderName NOT set in DTO
};

var savedTransaction = await AddAsync(transactionDto);

// ❌ BUG: ProviderName NEVER SET!
// No follow-up update like PayPal has!

// Update shipment IsPaid status
await _shipmentRepository.UpdateFields(...);
```

#### **Refund Flow Falls Back to PayPal**
```csharp
// Line 602-604: Refund payment logic
var providerName = !string.IsNullOrWhiteSpace(transaction.ProviderName) 
	? transaction.ProviderName 
	: "PayPal"; // ← Defaults to PayPal if NULL!

// Since Stripe transactions have ProviderName = NULL:
// providerName = "PayPal" ← WRONG!

var gateway = await _paymentGatewayFactory.GetGatewayByNameAsync(providerName);
// Returns PayPalPaymentGateway instead of StripePaymentGateway!
```

---

## ✅ Solution Implemented

### **Fix 1: Set ProviderName During Stripe Capture**

**File**: `Business/Services/PaymentTransactionService.cs`  
**Method**: `CaptureStripeAsync`  
**Lines**: After 418

**Before**:
```csharp
var savedTransaction = await AddAsync(transactionDto);
if (!savedTransaction.Success)
{
	return CreateErrorResult(500, "Failed to save payment transaction",
		"Payment was captured but transaction record could not be saved");
}

// Update shipment IsPaid status
await _shipmentRepository.UpdateFields(request.ShipmentId, s =>
```

**After**:
```csharp
var savedTransaction = await AddAsync(transactionDto);
if (!savedTransaction.Success)
{
	return CreateErrorResult(500, "Failed to save payment transaction",
		"Payment was captured but transaction record could not be saved");
}

// Set ProviderName for Stripe transactions (required for refunds)
var persistedTransaction = await _repository.GetById(savedTransaction.Id);
if (persistedTransaction != null)
{
	persistedTransaction.ProviderName = "Stripe";
	persistedTransaction.UpdatedDate = DateTime.UtcNow;
	persistedTransaction.UpdatedBy = _userService.GetLoggedInUser();
	await _repository.Update(persistedTransaction);
}

// Update shipment IsPaid status
await _shipmentRepository.UpdateFields(request.ShipmentId, s =>
```

**Benefits**:
- ✅ Matches the PayPal capture pattern
- ✅ Ensures `ProviderName = "Stripe"` is always set
- ✅ Refund flow will now correctly select Stripe gateway
- ✅ Consistent behavior across all payment providers

---

### **Fix 2: Update Existing Stripe Transactions**

**File**: `fix-stripe-provider-name.sql`

Run this SQL script to fix **existing** Stripe transactions in your database:

```sql
-- Update existing Stripe transactions (identified by 'pi_' prefix)
UPDATE TbPaymentTransaction
SET ProviderName = 'Stripe',
	UpdatedDate = GETDATE(),
	UpdatedBy = 'System Migration'
WHERE ProviderName IS NULL
  AND TransactionReference LIKE 'pi_%'
  AND TransactionReference IS NOT NULL;
```

**Why This Works**:
- Stripe PaymentIntent IDs always start with `pi_`
- Stripe Charge IDs start with `ch_`
- PayPal Capture IDs have different patterns (e.g., `5TY...`, `6GK...`)
- This pattern matching is safe and accurate

---

## 🧪 Testing Plan

### **Step 1: Apply Code Changes**
```powershell
# Option A: Hot Reload (if debugger is active)
# Press the Hot Reload button in Visual Studio

# Option B: Restart
# Stop debugger → Build → Start
```

### **Step 2: Fix Existing Data**
```sql
-- Connect to your database
-- Run fix-stripe-provider-name.sql

-- Verify results
SELECT 
	Id,
	TransactionReference,
	ProviderName,
	TransactionStatus
FROM TbPaymentTransaction
WHERE TransactionReference LIKE 'pi_%'
ORDER BY CreatedDate DESC;

-- All should now have ProviderName = 'Stripe'
```

### **Step 3: Test Refund on Existing Stripe Payment**
1. Find a Stripe payment in your database
2. Verify `ProviderName = 'Stripe'` (after running SQL fix)
3. Trigger refund via API or admin panel:
```json
POST /api/Payment/Refund
{
  "shipmentId": "your-shipment-guid",
  "reason": "Test refund"
}
```
4. **Expected Result**: ✅ Success!
   - `providerName` variable = `"Stripe"` (not `"PayPal"`)
   - Gateway factory returns `StripePaymentGateway`
   - Refund processed through Stripe API
   - Stripe dashboard shows refund

### **Step 4: Test New Stripe Payment**
1. Create a **new** Stripe payment
2. Capture it
3. Verify in database:
```sql
SELECT TOP 1
	Id,
	TransactionReference,
	ProviderName,  -- Should be 'Stripe' automatically!
	TransactionStatus,
	CreatedDate
FROM TbPaymentTransaction
WHERE TransactionReference LIKE 'pi_%'
ORDER BY CreatedDate DESC;
```
4. Trigger refund
5. **Expected Result**: ✅ Works perfectly!

### **Step 5: Verify PayPal Still Works**
1. Create PayPal payment
2. Capture it
3. Verify `ProviderName = 'PayPal'`
4. Refund it
5. **Expected Result**: ✅ Still works as before!

---

## 📊 Data Migration Impact

### **Before Fix**:
```
TbPaymentTransaction
┌─────────┬──────────────────┬──────────────┬────────┐
│ ID      │ TransactionRef   │ ProviderName │ Status │
├─────────┼──────────────────┼──────────────┼────────┤
│ guid-1  │ pi_3TVvzP...     │ NULL ❌      │ Paid   │ ← Stripe, broken refund
│ guid-2  │ 8CN38942DF...    │ PayPal ✅    │ Paid   │ ← PayPal, works
│ guid-3  │ pi_3TWabC...     │ NULL ❌      │ Paid   │ ← Stripe, broken refund
└─────────┴──────────────────┴──────────────┴────────┘
```

### **After Fix**:
```
TbPaymentTransaction
┌─────────┬──────────────────┬──────────────┬────────┐
│ ID      │ TransactionRef   │ ProviderName │ Status │
├─────────┼──────────────────┼──────────────┼────────┤
│ guid-1  │ pi_3TVvzP...     │ Stripe ✅    │ Paid   │ ← Fixed by SQL migration
│ guid-2  │ 8CN38942DF...    │ PayPal ✅    │ Paid   │ ← Unchanged
│ guid-3  │ pi_3TWabC...     │ Stripe ✅    │ Paid   │ ← Fixed by SQL migration
│ guid-4  │ pi_3TXnew...     │ Stripe ✅    │ Paid   │ ← New payment auto-set
└─────────┴──────────────────┴──────────────┴────────┘
```

---

## 🔄 Complete Fix Summary

### **All Fixes Applied**:

1. ✅ **Gateway Factory Method** (`PaymentTransactionService.cs` line 607)
   - Changed `GetGateway()` → `await GetGatewayByNameAsync()`

2. ✅ **PayPal Capture ID Validation** (`PayPalPaymentGateway.cs`)
   - Removed dangerous Order ID fallback

3. ✅ **Stripe ID Format Handling** (`StripePaymentGateway.cs`)
   - Support both `pi_xxx` and `ch_xxx`

4. ✅ **Stripe ProviderName Missing** (`PaymentTransactionService.cs` after line 418) ⭐ NEW
   - Set `ProviderName = "Stripe"` during capture

5. ✅ **Data Migration Script** (`fix-stripe-provider-name.sql`) ⭐ NEW
   - Fix existing Stripe transactions

---

## 🎯 Debug Walkthrough

### **What You Saw in Debugger**:

**Breakpoint at line 607**:
```csharp
var gateway = await _paymentGatewayFactory.GetGatewayByNameAsync(providerName);
```

**Variable values**:
```
transaction.TransactionReference = "pi_3TVvzP21f6FJ6DMw12YFjDkg"  ← Stripe ID
transaction.ProviderName = null  ← ❌ NULL!
providerName = "PayPal"  ← ❌ Fell back to default!
gateway = PayPalPaymentGateway  ← ❌ Wrong gateway!
```

### **What You'll See After Fix**:

**Variable values**:
```
transaction.TransactionReference = "pi_3TVvzP21f6FJ6DMw12YFjDkg"  ← Stripe ID
transaction.ProviderName = "Stripe"  ← ✅ SET!
providerName = "Stripe"  ← ✅ Correct!
gateway = StripePaymentGateway  ← ✅ Right gateway!
```

---

## 📝 Files Modified

1. **`Business/Services/PaymentTransactionService.cs`** ⭐⭐⭐ CRITICAL
   - Line 607: Changed factory method
   - After line 418: Added ProviderName setting for Stripe

2. **`Business/Services/PaymentGateways/PayPalPaymentGateway.cs`**
   - Fixed Capture ID validation

3. **`Business/Services/PaymentGateways/StripePaymentGateway.cs`**
   - Added Charge ID support

4. **`fix-stripe-provider-name.sql`** ⭐ NEW
   - Data migration script

5. **`PAYMENT_REFUND_FIX_SUMMARY.md`**
   - Documentation

6. **`STRIPE_PROVIDERNAME_FIX.md`** ⭐ NEW (this file)
   - Debug walkthrough and ProviderName fix

---

## 🚀 Next Steps

1. **Hot Reload** or **Restart Debugger**
2. **Run SQL migration** to fix existing data
3. **Test refund** on the current Stripe transaction
4. **Verify** in Stripe dashboard
5. **Create new test payment** to verify automatic ProviderName setting

---

**Root Cause**: Stripe capture flow didn't set `ProviderName`, causing refund logic to default to PayPal  
**Impact**: All Stripe refunds failed with "RESOURCE_NOT_FOUND"  
**Fix**: Mirror PayPal's pattern — set `ProviderName = "Stripe"` after saving transaction  
**Migration**: SQL script to fix existing transactions  
**Status**: ✅ Fixed and ready to test!
