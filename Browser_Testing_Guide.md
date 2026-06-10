# Payment System - Browser Testing Guide

This guide provides step-by-step instructions for testing the payment system in your browser.

---

## 🚀 PREREQUISITES

### 1. Start the Application
```powershell
# From workspace root: E:\MyShipping\
cd UI
dotnet run
```

Wait for the message: `Now listening on: https://localhost:[port]`

### 2. Prepare Test Accounts
You'll need:
- **Regular User Account** - to test user-facing features
- **Admin Account** - to test admin features

---

## 👤 PART 1: USER-FACING PAYMENT TESTS

### Test 1.1: Access Payment History

**Steps:**
1. Log in as a **regular user**
2. Navigate to: Payment History (check main menu or user dashboard)
   - Direct URL: `https://localhost:[port]/PaymentTransactions/Index`
3. Observe the page loads

**Expected Results:**
- ✅ Page loads without errors
- ✅ Page title shows "Payment History"
- ✅ Only YOUR transactions are visible (not other users')
- ✅ Transactions displayed in table format

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 1.2: Verify Transaction List Display

**On the Payment History page:**

**Check Column Headers:**
- ☐ #
- ☐ Transaction ID
- ☐ Date & Time
- ☐ Tracking Number
- ☐ Payment Method
- ☐ Amount
- ☐ Status
- ☐ Actions

**Check Data Display:**
- ☐ Dates formatted as: YYYY-MM-DD HH:MM
- ☐ Amounts show $ symbol (e.g., $45.00)
- ☐ Tracking numbers are NOT "N/A" (if shipment has tracking number)
- ☐ Payment methods show readable names (Credit Card, PayPal, etc.)
- ☐ Transaction IDs are shortened (not full GUID)

**Check Status Badges:**
- ☐ Pending = Yellow/Warning badge
- ☐ Completed = Green/Success badge
- ☐ Failed = Red/Danger badge
- ☐ Refunded = Gray/Secondary badge

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 1.3: Test Pagination

**Steps:**
1. If you have more than 10 transactions, pagination should appear
2. Check pagination controls at bottom of table
3. Click "Next" button
4. Check page 2 loads
5. Click a specific page number
6. Click "Previous" button

**Expected Results:**
- ✅ Pagination appears when needed
- ✅ Page numbers display correctly
- ✅ Current page is highlighted
- ✅ "Previous" disabled on page 1
- ✅ "Next" disabled on last page
- ✅ Page info shows: "Page X of Y — Showing Z of Total"

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 1.4: View Transaction Receipt/Details

**Steps:**
1. From payment history, click "View Receipt" on any transaction
2. Observe the details page

**Expected Results:**

**Transaction Information Card:**
- ☐ Transaction ID displays
- ☐ Transaction Reference displays (TXN-...)
- ☐ Status badge shows correct color
- ☐ Date displays
- ☐ Payment Method displays
- ☐ NO "N/A" values

**Shipment Information Card:**
- ☐ **Tracking Number displays** (NOT N/A) ← Critical test
- ☐ Origin displays
- ☐ Destination displays
- ☐ Shipping date displays

**Payment Breakdown:**
- ☐ Shipping Rate shows amount
- ☐ Commission shows (if applicable)
- ☐ Total matches transaction amount

**Other Elements:**
- ☐ Notes section (if any notes exist)
- ☐ "Back to Payment History" button works
- ☐ Educational disclaimer visible
- ☐ NO admin-only buttons (like "Process Refund")

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 1.5: Authorization Check

**Steps:**
1. Copy a transaction detail URL
2. Try to modify the transaction ID in URL to a different transaction (from another user if possible)
3. Observe the result

**Expected Results:**
- ✅ You should NOT be able to view other users' transactions
- ✅ You get access denied or redirected

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

## 👨‍💼 PART 2: ADMIN PAYMENT MANAGEMENT TESTS

### Test 2.1: Access Admin Payment List

**Steps:**
1. Log out and log in as **Admin user**
2. Navigate to Admin Panel
3. Look for "Payment Transactions" link in sidebar (under Configurations or Apps)
4. Click the link
   - Direct URL: `https://localhost:[port]/admin/PaymentTransactions/Index`

**Expected Results:**
- ✅ Link exists in admin sidebar
- ✅ Page loads without errors
- ✅ Page title shows "Payment Transactions" or "All Payment Transactions"
- ✅ Transactions from ALL users are visible (not just your own)

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.2: Verify Admin List Display

**Check Page Layout:**
- ☐ Filter controls at top (Search, Status dropdown, Payment Method dropdown, Date range, Search button)
- ☐ All filter controls are same height (large size)
- ☐ Date range shows as: [Start Date] **to** [End Date] (grouped with "to" separator)
- ☐ Search placeholder says "Search by Tracking Number"

**Check Table Display:**
- ☐ Counter column (#) increments: 1, 2, 3...
- ☐ All columns present: #, Transaction ID, Date & Time, Tracking Number, Payment Method, Amount, Status, Actions
- ☐ Tracking numbers display as colored badges
- ☐ Payment methods display correctly
- ☐ Amounts formatted as currency
- ☐ Status badges colored correctly
- ☐ Eye icon (👁) appears in Actions column

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.3: Test Search by Tracking Number (CRITICAL)

**Steps:**
1. Look at the transaction list and note a tracking number (e.g., 1234567890)
2. Type that tracking number into the search box
3. Click the search button (magnifying glass)
4. Observe results

**Expected Results:**
- ✅ The transaction with that tracking number appears
- ✅ Other transactions are filtered out
- ✅ **Search FINDS the transaction** (this was the bug we fixed)

**Test Variations:**
- ☐ Search exact full tracking number: Works
- ☐ Search partial tracking number (if supported): Works or shows "no results"
- ☐ Search non-existent tracking number: Shows "No payment transactions found"

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.4: Test Search by Transaction Reference

**Steps:**
1. Look at a transaction reference (TXN-20260421...)
2. Type part of it into search box (e.g., "TXN-202604")
3. Click search

**Expected Results:**
- ✅ Transaction found
- ✅ Partial match works

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.5: Test Status Filter

**Steps:**
1. Clear any existing filters
2. Select "Pending" from Status dropdown
3. Click search
4. Repeat for: Completed, Failed, Refunded, All Statuses

**Expected Results:**
- ✅ "All Statuses" shows all transactions
- ✅ "Pending" shows only Pending (yellow badge)
- ✅ "Completed" shows only Completed (green badge)
- ✅ "Failed" shows only Failed (red badge)
- ✅ "Refunded" shows only Refunded (gray badge)
- ✅ Selected status stays selected after filter applied

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.6: Test Payment Method Filter

**Steps:**
1. Clear filters
2. Select "Credit Card" from Payment Method dropdown
3. Click search
4. Repeat for: PayPal, Bank Transfer, Cash on Delivery, All Methods

**Expected Results:**
- ✅ "All Methods" shows all transactions
- ✅ Each method filter shows only transactions with that method
- ✅ Selected method stays selected after filter applied

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.7: Test Date Range Filter

**Steps:**
1. Clear filters
2. Set Start Date to a date in the past (e.g., 1 week ago)
3. Click search
4. Clear and set End Date to today
5. Click search
6. Set both Start Date and End Date (1 week range)
7. Click search

**Expected Results:**
- ✅ Start Date filters correctly (shows transactions on/after that date)
- ✅ End Date filters correctly (shows transactions on/before that date)
- ✅ Date range works (shows transactions within range)
- ✅ Dates are inclusive

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.8: Test Combined Filters

**Steps:**
1. Select Status = "Completed"
2. Select Payment Method = "Credit Card"
3. Enter a date range
4. Click search

**Expected Results:**
- ✅ Only transactions matching ALL criteria appear
- ✅ Filters work together correctly

**Test More Combinations:**
- ☐ Search + Status
- ☐ Search + Payment Method
- ☐ Search + Status + Payment Method + Date Range (all together)

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.9: Test Pagination with Filters

**Steps:**
1. Apply a filter that yields multiple pages
2. Navigate to page 2
3. Observe URL and results

**Expected Results:**
- ✅ Filter parameters persist in URL (page=2&status=1&...)
- ✅ Filtered results paginate correctly
- ✅ Page count reflects filtered results, not all transactions

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.10: View Admin Transaction Details

**Steps:**
1. From admin payment list, click eye icon (View Details) on any transaction
2. Observe the details page

**Expected Results:**

**Transaction Information:**
- ☐ All transaction fields display
- ☐ Status badge correct
- ☐ Payment method displays
- ☐ NO "N/A" values

**Shipment Information:**
- ☐ **Tracking Number displays correctly** (NOT N/A) ← Critical fix verification
- ☐ Origin and destination display
- ☐ Recipient info displays

**Payment Breakdown:**
- ☐ Breakdown displays correctly
- ☐ Amounts match

**Refund Button Visibility:**
- ☐ If transaction is Completed: "Process Refund" button appears
- ☐ If transaction is Pending/Failed/Refunded: NO refund button

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.11: Process Refund (CRITICAL WORKFLOW)

**Prerequisites:** Find or create a Completed transaction

**Steps:**
1. Go to admin transaction details for a Completed transaction
2. Verify "Process Refund" button is visible
3. Click "Process Refund"
4. Observe modal opens
5. Try to submit without reason
6. Enter a reason (e.g., "Customer requested refund")
7. Click Confirm

**Expected Results:**
- ✅ Modal opens with refund form
- ✅ Reason field is required (validation error if empty)
- ✅ Submitting with reason processes refund
- ✅ Success message displays
- ✅ Redirected back to details page
- ✅ Status badge now shows "Refunded" (gray)
- ✅ Notes section shows refund reason with timestamp
- ✅ "Process Refund" button no longer visible

**Verify in List:**
1. Go back to admin payment list
2. Filter by Status = "Refunded"
3. The refunded transaction appears in list

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 2.12: Refund Validation

**Try to refund:**
- ☐ Pending transaction → Should show error or no button
- ☐ Failed transaction → Should show error or no button
- ☐ Already Refunded transaction → Should show error or no button

**Expected Results:**
- ✅ Can only refund Completed transactions
- ✅ Validation prevents invalid refunds

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

## 🔍 PART 3: EDGE CASES & ERROR HANDLING

### Test 3.1: Empty States

**User Payment History:**
- [ ] Log in as a new user with no shipments/payments
- [ ] Navigate to payment history
- [ ] Expected: "No payment transactions found" message

**Admin Filters with No Results:**
- [ ] Apply filters that yield no results (e.g., status=Failed, method=PayPal, date from future)
- [ ] Expected: "No payment transactions found" message

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 3.2: Invalid Transaction ID

**Steps:**
1. Navigate to transaction details with fake GUID:
   - `/PaymentTransactions/Details/00000000-0000-0000-0000-000000000000`
2. Observe result

**Expected Results:**
- ✅ Error page or 404
- ✅ User-friendly message (not technical exception)

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 3.3: Unauthorized Access

**Steps:**
1. Log in as regular user
2. Try to access admin payment URL directly:
   - `/admin/PaymentTransactions/Index`
3. Observe result

**Expected Results:**
- ✅ Access denied
- ✅ Redirected to login or unauthorized page

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

### Test 3.4: Browser Console Check

**Steps:**
1. Open browser developer tools (F12)
2. Go to Console tab
3. Navigate through payment pages
4. Observe console

**Expected Results:**
- ✅ No JavaScript errors
- ✅ No broken resource errors (404 for CSS/JS files)
- ✅ Clean console

**Record Result:** Pass ☐ / Fail ☐  
**Notes:**

---

## 📊 TEST RESULTS SUMMARY

### User-Facing Features
- Total Tests: 5
- Passed: ___
- Failed: ___

### Admin Features
- Total Tests: 12
- Passed: ___
- Failed: ___

### Edge Cases
- Total Tests: 4
- Passed: ___
- Failed: ___

### OVERALL RESULT: ___% Pass Rate

---

## 🐛 ISSUES FOUND

### Critical Issues:
1. 

### Minor Issues:
1. 

### Enhancements Suggested:
1. 

---

## ✅ FINAL SIGN-OFF

All critical features tested: ☐ Yes / ☐ No  
All critical issues resolved: ☐ Yes / ☐ No  
System ready for next phase: ☐ Yes / ☐ No  

**Tested By:** ________________  
**Date:** ________________  
**Signature:** ________________

