-- Fix Existing Stripe Transactions
-- Run this SQL script to update existing Stripe transactions that don't have ProviderName set

-- This identifies Stripe transactions by their TransactionReference pattern (starts with 'pi_')
UPDATE TbPaymentTransaction
SET ProviderName = 'Stripe',
	UpdatedDate = GETDATE(),
	UpdatedBy = 'System Migration'
WHERE ProviderName IS NULL
  AND TransactionReference LIKE 'pi_%'
  AND TransactionReference IS NOT NULL;

-- Check the results
SELECT 
	Id,
	ShipmentId,
	TransactionReference,
	ProviderName,
	TransactionStatus,
	CreatedDate
FROM TbPaymentTransaction
WHERE TransactionReference LIKE 'pi_%'
ORDER BY CreatedDate DESC;

-- Optional: Fix any PayPal transactions with Capture IDs but no ProviderName
-- (PayPal Capture IDs typically start with specific patterns)
UPDATE TbPaymentTransaction
SET ProviderName = 'PayPal',
	UpdatedDate = GETDATE(),
	UpdatedBy = 'System Migration'
WHERE ProviderName IS NULL
  AND TransactionReference IS NOT NULL
  AND TransactionReference NOT LIKE 'pi_%'
  AND TransactionReference NOT LIKE 'ch_%';

-- Verify all transactions now have ProviderName
SELECT 
	ProviderName,
	COUNT(*) as TransactionCount,
	MIN(CreatedDate) as OldestTransaction,
	MAX(CreatedDate) as NewestTransaction
FROM TbPaymentTransaction
WHERE TransactionReference IS NOT NULL
GROUP BY ProviderName
ORDER BY ProviderName;
