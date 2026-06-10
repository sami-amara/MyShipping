-- Payment System Test Data Verification Script
-- Run this in SQL Server Management Studio or Azure Data Studio
-- Connected to: localhost.Shipping

USE Shipping;
GO

-- ========================================
-- 1. VERIFY PAYMENT METHODS
-- ========================================
PRINT '=== PAYMENT METHODS ===';
SELECT 
	Id,
	MethodEname AS [Method Name],
	CurrentState AS [Active]
FROM TbPaymentMethod
WHERE CurrentState > 0
ORDER BY MethodEname;

PRINT '';
PRINT 'Expected: Credit Card, PayPal, Bank Transfer, Cash on Delivery';
PRINT '';

-- ========================================
-- 2. VERIFY PAYMENT TRANSACTIONS
-- ========================================
PRINT '=== PAYMENT TRANSACTIONS SUMMARY ===';
SELECT 
	COUNT(*) AS [Total Transactions],
	SUM(CASE WHEN TransactionStatus = 0 THEN 1 ELSE 0 END) AS [Pending],
	SUM(CASE WHEN TransactionStatus = 1 THEN 1 ELSE 0 END) AS [Completed],
	SUM(CASE WHEN TransactionStatus = 2 THEN 1 ELSE 0 END) AS [Failed],
	SUM(CASE WHEN TransactionStatus = 3 THEN 1 ELSE 0 END) AS [Refunded]
FROM TbPaymentTransaction
WHERE CurrentState > 0;

PRINT '';

-- ========================================
-- 3. PAYMENT TRANSACTIONS BY PAYMENT METHOD
-- ========================================
PRINT '=== TRANSACTIONS BY PAYMENT METHOD ===';
SELECT 
	pm.MethodEname AS [Payment Method],
	COUNT(pt.Id) AS [Transaction Count]
FROM TbPaymentMethod pm
LEFT JOIN TbPaymentTransaction pt ON pm.Id = pt.PaymentMethodId AND pt.CurrentState > 0
WHERE pm.CurrentState > 0
GROUP BY pm.MethodEname
ORDER BY COUNT(pt.Id) DESC;

PRINT '';

-- ========================================
-- 4. SAMPLE PAYMENT TRANSACTIONS WITH DETAILS
-- ========================================
PRINT '=== SAMPLE TRANSACTIONS (First 10) ===';
SELECT TOP 10
	pt.Id AS [Transaction ID],
	pt.TransactionReference AS [Reference],
	pt.TransactionStatus AS [Status (0=Pending,1=Completed,2=Failed,3=Refunded)],
	pm.MethodEname AS [Payment Method],
	s.TrackingNumber AS [Tracking Number],
	pt.ShippingRate AS [Shipping Rate],
	pt.TotalAmount AS [Total Amount],
	pt.CreatedDate AS [Created],
	pt.Notes
FROM TbPaymentTransaction pt
LEFT JOIN TbPaymentMethod pm ON pt.PaymentMethodId = pm.Id
LEFT JOIN TbShippment s ON pt.ShipmentId = s.Id
WHERE pt.CurrentState > 0
ORDER BY pt.CreatedDate DESC;

PRINT '';

-- ========================================
-- 5. CHECK FOR TRANSACTIONS WITH MISSING DATA
-- ========================================
PRINT '=== DATA QUALITY CHECKS ===';
SELECT 
	'Transactions without Shipment' AS [Issue],
	COUNT(*) AS [Count]
FROM TbPaymentTransaction
WHERE ShipmentId IS NULL AND CurrentState > 0
UNION ALL
SELECT 
	'Transactions without Payment Method' AS [Issue],
	COUNT(*) AS [Count]
FROM TbPaymentTransaction
WHERE PaymentMethodId IS NULL AND CurrentState > 0
UNION ALL
SELECT 
	'Transactions without Transaction Reference' AS [Issue],
	COUNT(*) AS [Count]
FROM TbPaymentTransaction
WHERE TransactionReference IS NULL AND CurrentState > 0;

PRINT '';

-- ========================================
-- 6. VERIFY SHIPMENTS HAVE TRACKING NUMBERS
-- ========================================
PRINT '=== SHIPMENTS WITH PAYMENTS ===';
SELECT 
	COUNT(DISTINCT s.Id) AS [Total Shipments with Payments],
	COUNT(DISTINCT CASE WHEN s.TrackingNumber IS NOT NULL THEN s.Id END) AS [Shipments with Tracking Numbers],
	COUNT(DISTINCT CASE WHEN s.TrackingNumber IS NULL THEN s.Id END) AS [Shipments without Tracking Numbers]
FROM TbShippment s
INNER JOIN TbPaymentTransaction pt ON s.Id = pt.ShipmentId
WHERE s.CurrentState > 0 AND pt.CurrentState > 0;

PRINT '';

-- ========================================
-- 7. TRANSACTIONS BY USER (TOP 5 USERS)
-- ========================================
PRINT '=== TOP 5 USERS WITH MOST TRANSACTIONS ===';
SELECT TOP 5
	s.CreatedBy AS [User ID],
	COUNT(pt.Id) AS [Transaction Count],
	SUM(pt.TotalAmount) AS [Total Amount]
FROM TbPaymentTransaction pt
INNER JOIN TbShippment s ON pt.ShipmentId = s.Id
WHERE pt.CurrentState > 0 AND s.CurrentState > 0
GROUP BY s.CreatedBy
ORDER BY COUNT(pt.Id) DESC;

PRINT '';

-- ========================================
-- 8. REFUNDED TRANSACTIONS (IF ANY)
-- ========================================
PRINT '=== REFUNDED TRANSACTIONS ===';
SELECT 
	pt.Id,
	pt.TransactionReference,
	s.TrackingNumber,
	pt.TotalAmount,
	pt.Notes AS [Refund Notes],
	pt.UpdatedDate AS [Refund Date]
FROM TbPaymentTransaction pt
LEFT JOIN TbShippment s ON pt.ShipmentId = s.Id
WHERE pt.TransactionStatus = 3 AND pt.CurrentState > 0
ORDER BY pt.UpdatedDate DESC;

PRINT '';
PRINT '=== END OF VERIFICATION SCRIPT ===';
