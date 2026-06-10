-- Query to check PayPal webhook events received
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

-- Query to check payment transactions
SELECT TOP 10
	Id,
	Amount,
	Currency,
	Status,
	PaymentMethodId,
	TransactionId,
	CreatedAt,
	ExternalTransactionId
FROM PaymentTransactions
ORDER BY CreatedAt DESC;

-- Query to check if webhook reconciled a transaction
SELECT 
	wh.EventId,
	wh.EventType,
	wh.ReceivedAt,
	wh.IsProcessed,
	pt.TransactionId,
	pt.Status,
	pt.Amount
FROM WebhookEvents wh
LEFT JOIN PaymentTransactions pt ON wh.TransactionId = pt.Id
WHERE wh.Provider = 'PayPal'
ORDER BY wh.ReceivedAt DESC;
