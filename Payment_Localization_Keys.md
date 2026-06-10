# Payment Views - Hardcoded Strings Extraction

## Required Resource Keys for Localization

### **Common/Shared Keys**
- PaymentTransactions
- PaymentHistory
- PaymentReceipt  
- PaymentTransaction
- PaymentTransactionDetails
- AllPaymentTransactions
- Management
- Configurations

### **Table Headers / List Columns**
- TransactionID
- DateTime
- TrackingNumber
- PaymentMethod
- Amount
- Status
- Actions
- TransactionReference
- Reference

### **Status Values**
- Pending
- Completed
- Failed
- Refunded
- Unknown

### **Filter / Search Labels**
- Search
- SearchByTrackingNumber
- AllStatuses
- AllMethods
- StartDate
- EndDate
- DateRange
- FilterTransactions

### **Card/Section Titles**
- TransactionInformation
- ShipmentInformation
- PaymentBreakdown
- Notes
- Error

### **Payment Breakdown Fields**
- ShippingRate
- Commission
- TotalAmount
- ShipmentID

### **Action Buttons**
- ViewDetails
- ViewReceipt
- BackToList
- BackToPaymentHistory
- ProcessRefund
- ConfirmRefund
- Cancel
- Previous
- Next

### **Refund Related**
- Refund
- RefundReason
- RefundReasonRequired
- RefundWarning
- RefundCannotBeUndone
- TransactionSummary
- EnterRefundReason

### **Placeholders**
- SearchPlaceholder (e.g., "Search tracking number or reference...")
- RefundReasonPlaceholder (e.g., "Enter reason for refund...")

### **Messages**
- NoTransactionsFound
- TransactionNotFound
- ReturnToPaymentTransactions
- EducationalSystem
- EducationalPaymentDisclaimer (e.g., "This is a simulated payment transaction...")

### **Date Formatting**
- Page
- Showing
- Of

### **Admin Specific**
- AllTransactions
- FilterBy

---

## Extracted from Each View:

### **1. UI/Views/PaymentTransactions/Index.cshtml (User Payment History)**
Hardcoded strings:
- "Payment History"
- "My Transactions"
- "#"
- "Transaction ID"
- "Date & Time"
- "Tracking Number"
- "Payment Method"
- "Amount"
- "Status"
- "Actions"
- "View Receipt"
- "No payment transactions found"
- "Previous"
- "Next"
- "Page"
- "of"
- "Showing"

### **2. UI/Views/PaymentTransactions/Details.cshtml (User Receipt)**
Hardcoded strings:
- "Payment Receipt"
- "Payment Transactions"
- "Receipt Details"
- "Transaction Information"
- "Shipment Information"
- "Payment Breakdown"
- "Notes"
- "Error"
- "Transaction ID"
- "Date & Time"
- "Payment Method"
- "Reference"
- "Tracking Number"
- "Shipment ID"
- "N/A"
- "Shipping Rate"
- "Commission"
- "Total Amount"
- "Back to Payment History"
- "Educational System"
- "This is a simulated payment transaction for learning purposes only..."

### **3. UI/Areas/admin/Views/PaymentTransactions/Index.cshtml (Admin List)**
Hardcoded strings:
- "Payment Transactions"
- "Management"
- "All Payment Transactions"
- "Search by Tracking Number"
- "All Statuses"
- "Pending"
- "Completed"
- "Failed"
- "Refunded"
- "All Methods"
- "Start Date"
- "End Date"
- "to"
- "#"
- "Transaction ID"
- "Date & Time"
- "Tracking Number"
- "Payment Method"
- "Amount"
- "Status"
- "Actions"
- "No payment transactions found"
- "Previous"
- "Next"
- "Page"
- "of"
- "Showing"

### **4. UI/Areas/admin/Views/PaymentTransactions/Details.cshtml (Admin Details + Refund)**
Hardcoded strings:
- "Payment Receipt"
- "Payment Transactions"
- "Details"
- "Transaction Details"
- "ID:"
- "Transaction Information"
- "Shipment Information"
- "Payment Breakdown"
- "Notes"
- "Error"
- "Date & Time:"
- "Payment Method:"
- "Reference:"
- "Tracking Number:"
- "Shipment ID:"
- "Shipping Rate"
- "Commission"
- "Total Amount"
- "Back to List"
- "Process Refund"
- "Educational System"
- "This is a simulated payment transaction for learning purposes only."
- "Process Refund" (modal title)
- "Warning:"
- "This action will refund the payment transaction and cannot be undone."
- "Refund Reason"
- "*" (required indicator)
- "Enter reason for refund (e.g., Customer request, Order cancelled, etc.)"
- "Transaction Summary"
- "Amount:"
- "Method:"
- "Cancel"
- "Confirm Refund"
- "Transaction Not Found"
- "The requested payment transaction could not be found."
- "Return to Payment Transactions"

### **5. Controllers - TempData Messages**

**UI/Controllers/PaymentTransactionsController.cs:**
- "Payment transaction not found"

**UI/Areas/admin/Controllers/PaymentTransactionsController.cs:**
- "Failed to load payment transactions"
- "Transaction not found"
- "Failed to load transaction details"
- "Refund reason is required"
- "Payment refund completed successfully. The transaction status has been updated to 'Refunded'."
- "Failed to process refund. Please try again."

---

## Total Unique Keys Needed: ~75-80

---

## Naming Convention to Follow:
Based on existing patterns in Labels.resx:
- PascalCase without spaces
- Descriptive and clear
- Group related keys by prefix (e.g., Payment*, Refund*, Transaction*)

Examples:
- PaymentTransactions
- TransactionID
- PaymentMethod
- AllStatuses
- ProcessRefund
- RefundReason
