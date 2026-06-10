# Database Migration - AdditionalInfo Column

**Date:** April 25, 2026  
**Migration:** AddAdditionalInfoToPaymentTransaction  
**Status:** ✅ Successfully Applied

---

## Migration Details

### Migration Name
`20260425161854_AddAdditionalInfoToPaymentTransaction`

### Changes Applied
- **Table:** `TbPaymentTransaction`
- **Column Added:** `AdditionalInfo`
- **Data Type:** `nvarchar(max)`
- **Nullable:** `YES` (allows NULL values)

### Migration Files Created
1. `DataAccessLayer/Migrations/20260425161854_AddAdditionalInfoToPaymentTransaction.cs`
2. `DataAccessLayer/Migrations/20260425161854_AddAdditionalInfoToPaymentTransaction.Designer.cs`

---

## Verification Results ✅

### Database Schema Verification
```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'TbPaymentTransaction' 
AND COLUMN_NAME = 'AdditionalInfo'
```

**Result:**
| COLUMN_NAME | DATA_TYPE | IS_NULLABLE | CHARACTER_MAXIMUM_LENGTH |
|-------------|-----------|-------------|--------------------------|
| AdditionalInfo | nvarchar | YES | -1 (max) |

✅ Column successfully added to database

### Existing Data Check
Verified existing payment transactions - all have `AdditionalInfo = NULL` (as expected for old records):

| TransactionId | ProviderName | TransactionStatus | AdditionalInfo |
|---------------|--------------|-------------------|----------------|
| 2F581CB8-... | PayPal | 0 (Pending) | NULL |
| 5E35AC82-... | Stripe | 1 (Completed) | NULL |
| F747C96E-... | PayPal | 0 (Pending) | NULL |
| D4F7237B-... | PayPal | 0 (Pending) | NULL |
| FC6E1E9D-... | PayPal | 0 (Pending) | NULL |

✅ Existing records intact - no data corruption

---

## What This Column Is Used For

### Purpose
The `AdditionalInfo` column stores gateway-specific metadata that doesn't fit in standard fields:

1. **PayPal Approval URLs** - Stores the PayPal checkout URL for redirect-based payments
   - Example: `"https://www.sandbox.paypal.com/checkoutnow?token=8PU12345..."`

2. **Gateway-Specific Data** - Any additional information returned by payment providers
   - Capture IDs
   - Approval timestamps
   - Provider-specific transaction metadata

### Payment Flow Usage

#### Before Approval (Order Creation)
```csharp
// PayPal order created - approval required
transaction.TransactionStatus = 0; // Pending
transaction.TransactionReference = "8PU12345..."; // PayPal order ID
transaction.AdditionalInfo = "https://www.sandbox.paypal.com/checkoutnow?token=8PU12345...";
```

Frontend checks `AdditionalInfo`:
```javascript
if (transaction.AdditionalInfo && transaction.AdditionalInfo.startsWith('http')) {
	// Redirect user to PayPal for approval
	window.location.href = transaction.AdditionalInfo;
}
```

#### After Approval (Capture)
```csharp
// PayPal payment captured
transaction.TransactionStatus = 1; // Completed
transaction.TransactionReference = "9AB67890..."; // PayPal capture ID
transaction.AdditionalInfo = "Captured successfully. Original order: 8PU12345...";
```

---

## Database Table Complete Structure

### TbPaymentTransaction Columns (After Migration)

| Column Name | Data Type | Nullable | Description |
|-------------|-----------|----------|-------------|
| Id | uniqueidentifier | NO | Primary key |
| ShipmentId | uniqueidentifier | NO | FK to shipment |
| PaymentMethodId | uniqueidentifier | NO | FK to payment method |
| Amount | decimal | NO | Total payment amount |
| Currency | nvarchar | NO | Payment currency (USD) |
| TransactionStatus | int | NO | 0=Pending, 1=Completed, 2=Failed, 3=Refunded |
| TransactionReference | nvarchar | YES | Gateway transaction/order ID |
| ProcessedDate | datetime | YES | When payment was processed |
| ErrorMessage | nvarchar | YES | Error details if failed |
| Notes | nvarchar | YES | Additional transaction notes |
| IdempotencyKey | nvarchar | YES | Prevents duplicate payments |
| ProviderEventId | nvarchar | YES | Webhook event ID |
| ProviderName | nvarchar | YES | PayPal, Stripe, etc. |
| **AdditionalInfo** | **nvarchar(max)** | **YES** | **Gateway-specific metadata** ✨ NEW |
| CreatedBy | uniqueidentifier | NO | User who created |
| CreatedDate | datetime | NO | Creation timestamp |
| UpdatedBy | uniqueidentifier | YES | Last updated by |
| UpdatedDate | datetime | YES | Last update timestamp |
| CurrentState | int | NO | Active/Inactive flag |

---

## Rollback Instructions (If Needed)

If you ever need to remove this column:

```powershell
cd E:\MyShipping\DataAccessLayer
dotnet ef migrations remove --startup-project ..\WebApi\WebApi.csproj
```

Or create a down migration:
```powershell
dotnet ef database update <PreviousMigrationName> --startup-project ..\WebApi\WebApi.csproj
```

---

## Testing Recommendations

### 1. Test PayPal Order Creation
Create a new shipment with PayPal:
```sql
-- Check the new transaction record
SELECT 
	Id,
	TransactionReference,
	TransactionStatus,
	AdditionalInfo,
	CreatedDate
FROM TbPaymentTransaction
WHERE ProviderName = 'PayPal'
ORDER BY CreatedDate DESC;
```

Expected `AdditionalInfo`:
```
https://www.sandbox.paypal.com/checkoutnow?token=<ORDER_ID>
```

### 2. Test PayPal Capture
After user approves on PayPal:
```sql
-- Check updated transaction
SELECT 
	Id,
	TransactionReference,
	TransactionStatus,
	AdditionalInfo,
	ProcessedDate
FROM TbPaymentTransaction
WHERE TransactionStatus = 1 -- Completed
AND ProviderName = 'PayPal'
ORDER BY ProcessedDate DESC;
```

Expected `AdditionalInfo`:
```
Payment captured successfully via PayPal. Capture ID: <CAPTURE_ID>. Original order: <ORDER_ID>
```

### 3. Test Stripe Payments
Stripe payments won't use `AdditionalInfo` initially:
```sql
SELECT 
	Id,
	TransactionReference,
	TransactionStatus,
	AdditionalInfo
FROM TbPaymentTransaction
WHERE ProviderName = 'Stripe'
ORDER BY CreatedDate DESC;
```

Expected `AdditionalInfo`: `NULL` (unless extended later)

---

## Migration Log

### Executed Commands
```powershell
# 1. Create migration
cd E:\MyShipping\DataAccessLayer
dotnet ef migrations add AddAdditionalInfoToPaymentTransaction --startup-project ..\WebApi\WebApi.csproj

# 2. Apply migration
dotnet ef database update --startup-project ..\WebApi\WebApi.csproj
```

### Output
```
Done. To undo this action, use 'ef migrations remove'
```

### SQL Executed
```sql
ALTER TABLE [TbPaymentTransaction] 
ADD [AdditionalInfo] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260425161854_AddAdditionalInfoToPaymentTransaction', N'9.0.10');
```

---

## Summary

✅ Migration created successfully  
✅ Database updated successfully  
✅ Column `AdditionalInfo` added to `TbPaymentTransaction`  
✅ Existing data preserved (all old records have `NULL` in new column)  
✅ Schema verified via SQL queries  
✅ Ready for PayPal approval URL storage  

**Next Step:** Test the complete PayPal payment flow end-to-end!

---

**Related Documents:**
- [OPTION_A_IMPLEMENTATION_COMPLETE.md](OPTION_A_IMPLEMENTATION_COMPLETE.md) - Complete implementation guide
- [PAYPAL_INTEGRATION_PROGRESS.md](PAYPAL_INTEGRATION_PROGRESS.md) - Session progress
- [PAYPAL_PAYMENT_FLOW_CHANGES.md](PAYPAL_PAYMENT_FLOW_CHANGES.md) - Design rationale
