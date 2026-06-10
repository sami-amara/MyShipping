# Payment Flow Testing Guide - Educational Project

## 🎓 What You've Built

A complete **simulated payment system** for learning purposes that demonstrates:
- Payment transaction logging
- Commission calculation
- User feedback with animations
- Payment receipt generation
- Transaction history

---

## ✅ Testing Checklist

### **1. Database Verification**
Check that the `TbPaymentTransaction` table exists:

```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'TbPaymentTransaction';

-- View table structure
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TbPaymentTransaction';
```

### **2. Payment Methods Setup**
Verify payment methods exist with commissions:

```sql
SELECT Id, MethodEname, Commission, CurrentState
FROM TbPaymentMethods
WHERE IsDelete = 0 AND CurrentState = 1;
```

If no payment methods exist, insert test data:

```sql
-- Already seeded in migration, but you can add more:
INSERT INTO TbPaymentMethods (Id, MethdAname, MethodEname, Commission, CurrentState, CreatedDate, CreatedBy, IsDelete)
VALUES 
  (NEWID(), 'بطاقة ائتمان', 'Credit Card', 2.5, 1, GETDATE(), '00000000-0000-0000-0000-000000000000', 0),
  (NEWID(), 'نقدي', 'Cash', 0, 1, GETDATE(), '00000000-0000-0000-0000-000000000000', 0);
```

---

## 🧪 Manual Testing Steps

### **Test 1: Complete Shipment Creation with Payment**

1. **Start the application**
   ```powershell
   cd E:\MyShipping\WebApi
   dotnet run
   ```

2. **Navigate to**: https://localhost:7065/Shipments/Create

3. **Fill out the form step by step**:
   - **Step 0 (Sender)**: Enter sender details
   - **Step 1 (Receiver)**: Enter receiver details
   - **Step 2 (Package)**: Enter package dimensions and value
   - **Step 3 (Shipping)**: Select shipping type and dates
   - **Step 4 (Payment)**: ✅ **Select a payment method** ← KEY STEP
   - **Step 5 (Review)**: Verify payment summary shows:
	 - Shipping rate
	 - Commission amount
	 - Total amount
   - **Step 6 (Submit)**: 
	 - Watch for payment processing animation (1.5 seconds)
	 - See payment receipt with breakdown

4. **Expected Results**:
   - ✅ Shipment created successfully
   - ✅ Payment transaction logged in database
   - ✅ Payment receipt displayed
   - ✅ No errors in console

### **Test 2: Payment Method Validation**

1. Navigate to shipment creation
2. Fill out steps 0-3
3. On **Step 4 (Payment)**: Do NOT select a payment method
4. Click **Next** button
5. **Expected**: 
   - ⚠️ Warning message: "Please select a payment method"
   - ❌ Cannot proceed to review step

### **Test 3: Verify Payment Transaction in Database**

After creating a shipment, check the database:

```sql
-- Get the latest payment transaction
SELECT TOP 10
	pt.Id,
	pt.ShipmentId,
	pt.TransactionReference,
	pt.ShippingRate,
	pt.CommissionPercentage,
	pt.CommissionAmount,
	pt.TotalAmount,
	pt.TransactionStatus,
	pt.ProcessedDate,
	pm.MethodEname AS PaymentMethod,
	s.TrackingNumber
FROM TbPaymentTransaction pt
LEFT JOIN TbPaymentMethods pm ON pt.PaymentMethodId = pm.Id
LEFT JOIN TbShippments s ON pt.ShipmentId = s.Id
ORDER BY pt.CreatedDate DESC;
```

**Expected Results**:
- Transaction exists with unique reference (TXN-{timestamp}-{random})
- Commission calculated correctly
- Total = ShippingRate + CommissionAmount
- TransactionStatus = 1 (Completed) in 95% of cases
- TransactionStatus = 2 (Failed) in ~5% of cases (simulated)

### **Test 4: Payment API Endpoints**

Test using Swagger or Postman:

**A. Get Payment by Shipment ID**
```
GET /api/PaymentTransactions/shipment/{shipmentId}
Authorization: Bearer {your-jwt-token}
```

**B. Get Payment History**
```
GET /api/PaymentTransactions/my-history
Authorization: Bearer {your-jwt-token}
```

**C. Process Payment (Direct API call)**
```
POST /api/PaymentTransactions/process
Authorization: Bearer {your-jwt-token}
Content-Type: application/json

{
  "shipmentId": "{guid}",
  "paymentMethodId": "{guid}",
  "shippingRate": 50.00
}
```

**D. Simulate Refund**
```
POST /api/PaymentTransactions/{transactionId}/refund
Authorization: Bearer {your-jwt-token}
Content-Type: application/json

{
  "reason": "Customer requested refund"
}
```

---

## 🎯 Success Criteria

### ✅ **Functional Requirements**
- [ ] Payment method dropdown populates
- [ ] Cannot proceed without selecting payment method
- [ ] Payment summary displays correctly in review
- [ ] Payment processing animation shows
- [ ] Payment receipt displays with all details
- [ ] Transaction logged in database
- [ ] Commission calculated correctly (e.g., 2.5% of $50 = $1.25, Total = $51.25)

### ✅ **Educational Goals**
- [ ] Clear understanding of transaction logging pattern
- [ ] Understanding of simulated payment flow
- [ ] Knowledge of commission calculation
- [ ] Understanding of UI/UX for payment processing
- [ ] Experience with transaction states (Pending/Completed/Failed/Refunded)

---

## 📊 Sample Test Data

### **Example Payment Calculation**

Given:
- Shipping Rate: $50.00
- Payment Method: Credit Card (2.5% commission)

Expected:
- Commission Amount: $50.00 × 2.5% = $1.25
- Total Amount: $50.00 + $1.25 = $51.25

---

## 🐛 Troubleshooting

### **Issue: Payment method dropdown is empty**
**Solution**: Check if payment methods exist in database (see section 2)

### **Issue: Payment transaction not created**
**Solution**: 
1. Check browser console for errors
2. Check server logs
3. Verify PaymentMethodId is not empty/null

### **Issue: Commission not calculated**
**Solution**: Verify payment method has Commission value set in database

### **Issue: Build errors**
**Solution**: Rebuild solution
```powershell
dotnet build
```

---

## 📝 Learning Points

### **1. Transaction Patterns**
- Always use database transactions for multi-step operations
- Rollback on failure to maintain data integrity

### **2. Simulated vs Real Payment**
- **Simulated** (this project): Automatic success/failure, no real gateway
- **Real** (production): Integrate Stripe, PayPal, etc.

### **3. Payment States**
```
Pending → Processing → Completed ✓
					 ↘ Failed ✗

Completed → Refunded
```

### **4. Security Considerations (Not Implemented - For Learning)**
In production, you would add:
- PCI compliance for card data
- Encrypted payment information
- Webhook validation from payment gateways
- Fraud detection
- 3D Secure authentication

---

## 🎓 Next Learning Steps (Optional)

1. **Add Payment Gateway Integration**
   - Stripe API
   - PayPal REST API

2. **Add Payment History Page**
   - View all transactions
   - Filter by status
   - Export to PDF

3. **Add Refund Functionality**
   - Admin can refund transactions
   - Partial refunds
   - Refund tracking

4. **Add Payment Notifications**
   - Email receipts
   - SMS notifications
   - Webhook callbacks

---

## ✅ Testing Complete

Once all tests pass, you have successfully:
- ✅ Implemented a complete payment transaction system
- ✅ Understood payment flow architecture
- ✅ Learned transaction logging patterns
- ✅ Experienced simulated payment processing
- ✅ Built educational foundations for real payment integration

**Congratulations! Your educational payment system is complete!** 🎉
