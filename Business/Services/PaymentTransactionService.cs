using AutoMapper;
using Business.Contracts;
using Business.DTOS;
using Business.Payments.Gateway;
using Business.Payments.PayPal;
using Business.Payments.Stripe;
using Business.Payments.Shared;
using DataAccessLayer.Contracts;
using DataAccessLayer.Model;
using Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;

namespace Business.Services
{
    /// <summary>
    /// Payment Transaction Service - Real Payment Gateway Integration
    /// Integrates with Stripe, PayPal, and other payment providers
    /// </summary>
    public class PaymentTransactionService : BaseService<TbPaymentTransaction, PaymentTransactionDto>, IPaymentTransactionService
    {
        private readonly IGenericRepository<TbPaymentTransaction> _repository;
        private readonly IGenericRepository<TbPaymentMethod> _paymentMethodRepository;
        private readonly IGenericRepository<TbShippment> _shipmentRepository;
        private readonly IGenericRepository<TbPaymentWebhookEvent> _webhookEventRepository;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IPaymentGatewayFactory _paymentGatewayFactory;

        public PaymentTransactionService(
            IGenericRepository<TbPaymentTransaction> repository,
            IGenericRepository<TbPaymentMethod> paymentMethodRepository,
            IGenericRepository<TbShippment> shipmentRepository,
            IGenericRepository<TbPaymentWebhookEvent> webhookEventRepository,
            IMapper mapper,
            IUserService userService,
            IPaymentGatewayFactory paymentGatewayFactory) : base(repository, mapper, userService)
        {
            _repository = repository;
            _paymentMethodRepository = paymentMethodRepository;
            _shipmentRepository = shipmentRepository;
            _webhookEventRepository = webhookEventRepository;
            _mapper = mapper;
            _userService = userService;
            _paymentGatewayFactory = paymentGatewayFactory;
        }

        #region Private Helper Methods

        /// <summary>
        /// Validates that the shipment ID is not empty
        /// </summary>
        private PaymentOrchestrationResult? ValidateShipmentId(Guid shipmentId)
        {
            if (shipmentId == Guid.Empty)
                return CreateErrorResult(400, "Shipment ID is required");
            return null;
        }

        /// <summary>
        /// Validates that the amount is greater than zero
        /// </summary>
        private PaymentOrchestrationResult? ValidateAmount(decimal amount)
        {
            if (amount <= 0)
                return CreateErrorResult(400, "Amount must be greater than zero");
            return null;
        }

        /// <summary>
        /// Validates that the payment method ID is not empty
        /// </summary>
        private PaymentOrchestrationResult? ValidatePaymentMethodId(Guid paymentMethodId)
        {
            if (paymentMethodId == Guid.Empty)
                return CreateErrorResult(400, "Payment Method ID is required");
            return null;
        }

        /// <summary>
        /// Validates that a string value is not null or empty
        /// </summary>
        private PaymentOrchestrationResult? ValidateRequiredString(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return CreateErrorResult(400, $"{fieldName} is required");
            return null;
        }

        /// <summary>
        /// Retrieves a shipment by ID or returns an error result
        /// </summary>
        private async Task<(TbShippment? shipment, PaymentOrchestrationResult? error)> GetShipmentOrError(Guid shipmentId)
        {
            var shipment = await _shipmentRepository.GetById(shipmentId);
            if (shipment == null)
            {
                var error = CreateErrorResult(404, "Shipment not found");
                return (null, error);
            }
            return (shipment, null);
        }

        /// <summary>
        /// Resolves a payment gateway by name and casts to the specified type
        /// </summary>
        private (T? gateway, PaymentOrchestrationResult? error) GetGatewayOrError<T>(string gatewayName) where T : class
        {
            var gateway = _paymentGatewayFactory.GetGateway(gatewayName);
            if (gateway == null)
            {
                return (null, CreateErrorResult(400, $"{gatewayName} gateway not configured"));
            }

            var typedGateway = gateway as T;
            if (typedGateway == null)
            {
                return (null, CreateErrorResult(400, "Invalid gateway type"));
            }

            return (typedGateway, null);
        }

        /// <summary>
        /// Retrieves the Stripe payment method ID from the database
        /// </summary>
        private async Task<(Guid? paymentMethodId, PaymentOrchestrationResult? error)> GetStripePaymentMethodIdOrError()
        {
            var paymentMethods = await _paymentMethodRepository.GetList<TbPaymentMethod>(
                filter: null,
                selector: pm => pm,
                orderBy: null,
                isDescending: false);
            var stripePaymentMethod = paymentMethods.FirstOrDefault(pm =>
                pm.MethodEname != null && (
                    pm.MethodEname.Contains("Stripe", StringComparison.OrdinalIgnoreCase) ||
                    pm.MethodEname.Contains("Visa", StringComparison.OrdinalIgnoreCase) ||
                    pm.MethodEname.Contains("MasterCard", StringComparison.OrdinalIgnoreCase) ||
                    pm.MethodEname.Contains("Card", StringComparison.OrdinalIgnoreCase)));
            if (stripePaymentMethod == null)
            {
                return (null, CreateErrorResult(400,
                    "Stripe payment method not configured in database. Please add a payment method with 'Stripe' or 'Card' in the name."));
            }

            return (stripePaymentMethod.Id, null);
        }

        /// <summary>
        /// Creates a standardized error result
        /// </summary>
        private PaymentOrchestrationResult CreateErrorResult(int statusCode, string error, string? details = null)
        {
            return new PaymentOrchestrationResult
            {
                Success = false,
                StatusCode = statusCode,
                Error = error,
                Details = details
            };
        }

        /// <summary>
        /// Creates a standardized exception result
        /// </summary>
        private PaymentOrchestrationResult CreateExceptionResult(Exception ex, string context)
        {
            return new PaymentOrchestrationResult
            {
                Success = false,
                StatusCode = 500,
                Error = $"An error occurred while {context}",
                Details = ex.Message
            };
        }

        #endregion

        public async Task<PaymentOrchestrationResult> CreateOrderAsync(PaymentOrderRequest request)
        {
            try
            {
                // Validate inputs
                var validationError = ValidateShipmentId(request.ShipmentId)
                                   ?? ValidateAmount(request.Amount);
                if (validationError != null) return validationError;

                // Get shipment
                var (shipment, shipmentError) = await GetShipmentOrError(request.ShipmentId);
                if (shipmentError != null) return shipmentError;

                // Get PayPal gateway
                var (gateway, gatewayError) = GetGatewayOrError<IPaymentGateway>("PayPal");
                if (gatewayError != null) return gatewayError;

                var paymentRequest = new PaymentRequest
                {
                    Amount = request.Amount,
                    Currency = request.Currency ?? "USD",
                    Description = $"MyShipping - Shipment {(shipment!.TrackingNumber?.ToString() ?? "N/A")}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "ShipmentId", request.ShipmentId.ToString() },
                        { "TrackingNumber", shipment.TrackingNumber?.ToString() ?? string.Empty },
                        { "PaymentMethodId", request.PaymentMethodId.ToString() }
                    }
                };

                var result = await gateway!.ProcessPayment(paymentRequest);
                if (!result.Success || string.IsNullOrEmpty(result.TransactionId))
                {
                    return CreateErrorResult(400, result.ErrorMessage ?? "Failed to create PayPal order");
                }

                return new PaymentOrchestrationResult
                {
                    Success = true,
                    StatusCode = 200,
                    OrderId = result.TransactionId,
                    Status = result.Status.ToString()
                };
            }
            catch (Exception ex)
            {
                return CreateExceptionResult(ex, "creating the payment order");
            }
        }

        public async Task<PaymentOrchestrationResult> CaptureOrderAsync(PaymentCaptureRequest request)
        {
            try
            {
                // Validate inputs
                var validationError = ValidateRequiredString(request.OrderId, "Order ID") ?? ValidateShipmentId(request.ShipmentId) ?? ValidatePaymentMethodId(request.PaymentMethodId);
                if (validationError != null) return validationError;

                // Get shipment
                var (shipment, shipmentError) = await GetShipmentOrError(request.ShipmentId);
                if (shipmentError != null) return shipmentError;

                // Get PayPal gateway
                var (paypalGateway, gatewayError) = GetGatewayOrError<PaymentGateways.PayPalPaymentGateway>("PayPal");
                if (gatewayError != null) return gatewayError;

                var captureResult = await paypalGateway!.CaptureOrder(request.OrderId);
                if (captureResult == null)
                return CreateErrorResult(500, "No response from PayPal gateway");
                if (!captureResult.Success)
                {
                    return new PaymentOrchestrationResult
                    {
                        Success = false,
                        StatusCode = 400,
                        Error = captureResult.ErrorMessage ?? "Failed to capture PayPal order",
                        Status = captureResult.Status.ToString()
                    };
                }

                var transactionDto = new PaymentTransactionDto
                {
                    ShipmentId = request.ShipmentId,
                    PaymentMethodId = request.PaymentMethodId,
                    ShippingRate = request.Amount,
                    CommissionPercentage = 0,
                    CommissionAmount = 0,
                    TotalAmount = request.Amount,
                    TransactionStatus = (int)captureResult.Status,
                    TransactionReference = captureResult.TransactionId,
                    ProcessedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    Notes = $"PayPal order captured via JS SDK. {captureResult.AdditionalInfo}",
                    AdditionalInfo = $"PayPal Order ID: {request.OrderId}"
                };

                var savedTransaction = await Add(transactionDto);
                if (!savedTransaction.Success)
                {
                    return CreateErrorResult(500, "Failed to save payment transaction",
                        "Payment was captured but transaction record could not be saved");
                }

                var persistedTransaction = await _repository.GetById(savedTransaction.Id);
                if (persistedTransaction != null)
                {
                    persistedTransaction.ProviderName = "PayPal";
                    persistedTransaction.UpdatedDate = DateTime.UtcNow;
                    persistedTransaction.UpdatedBy = _userService.GetLoggedInUser();
                    await _repository.Update(persistedTransaction);
                }

                // Update shipment IsPaid status only — use UpdateFields to avoid
                // overwriting other columns (e.g. PaymentMethodId) with unloaded nulls
                await _shipmentRepository.UpdateFields(shipment!.Id, s =>
                {
                    s.IsPaid = true;
                    s.PaymentMethodId = request.PaymentMethodId;
                    s.UpdatedDate = DateTime.UtcNow;
                    s.UpdatedBy = _userService.GetLoggedInUser();
                });
                return new PaymentOrchestrationResult
                {
                    Success = true,
                    StatusCode = 200,
                    TransactionId = savedTransaction.Id,
                    TransactionReference = captureResult.TransactionId,
                    Status = captureResult.Status.ToString(),
                    Message = "Payment captured successfully"
                };
            }
            catch (Exception ex)
            {
                return CreateExceptionResult(ex, "capturing the payment");
            }
        }

        /// <summary>
        /// Creates a Stripe PaymentIntent for a shipment payment
        /// </summary>
        public async Task<PaymentOrchestrationResult> CreateStripeIntentAsync(StripePaymentIntentRequest request)
        {
            try
            {
                // Validate inputs
                var validationError = ValidateShipmentId(request.ShipmentId)
                                   ?? ValidateAmount(request.Amount);
                if (validationError != null) return validationError;

                // Get Stripe gateway
                var (stripeGateway, gatewayError) = GetGatewayOrError<PaymentGateways.StripePaymentGateway>("Stripe");
                if (gatewayError != null) return gatewayError;

                var result = await stripeGateway!.CreatePaymentIntentAsync(request.ShipmentId, request.Amount);
                if (!result.Success)
                {
                    return CreateErrorResult(400, result.Error ?? "Failed to create Stripe PaymentIntent", result.Details);
                }

                return new PaymentOrchestrationResult
                {
                    Success = true,
                    StatusCode = 200,
                    ClientSecret = result.ClientSecret,
                    PaymentIntentId = result.PaymentIntentId,
                    Status = "Intent Created"
                };
            }
            catch (Exception ex)
            {
                return CreateExceptionResult(ex, "creating the Stripe payment intent");
            }
        }

        /// <summary>
        /// Captures a Stripe payment and marks shipment as paid
        /// </summary>
        public async Task<PaymentOrchestrationResult> CaptureStripeAsync(StripeCaptureRequest request)
        {
            try
            {
                // Validate inputs
                var validationError = ValidateShipmentId(request.ShipmentId)
                                   ?? ValidateRequiredString(request.PaymentIntentId, "PaymentIntent ID");
                if (validationError != null) return validationError;

                // Look up Stripe payment method from database
                var (stripePaymentMethodId, methodError) = await GetStripePaymentMethodIdOrError();
                if (methodError != null) return methodError;

                // Get Stripe gateway
                var (stripeGateway, gatewayError) = GetGatewayOrError<PaymentGateways.StripePaymentGateway>("Stripe");
                if (gatewayError != null) return gatewayError;

                // Capture/confirm payment
                var result = await stripeGateway!.CapturePaymentAsync(request.PaymentIntentId, request.ShipmentId, stripePaymentMethodId!.Value);
                if (!result.Success)
                {
                    if (result.StatusCode == 404)
                        return CreateErrorResult(404, result.ErrorMessage);
                    if (result.StatusCode == 400)
                        return CreateErrorResult(400, result.ErrorMessage);
                    return CreateErrorResult(result.StatusCode, result.ErrorMessage, result.AdditionalInfo);
                }

                // Create payment transaction record
                var transactionDto = new PaymentTransactionDto
                {
                    ShipmentId = request.ShipmentId,
                    PaymentMethodId = stripePaymentMethodId.Value,
                    ShippingRate = result.AmountCharged,
                    CommissionPercentage = 0,
                    CommissionAmount = 0,
                    TotalAmount = result.AmountCharged,
                    TransactionStatus = (int)result.Status,
                    TransactionReference = result.TransactionReference ?? result.TransactionId,
                    ProcessedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    Notes = $"Stripe payment captured. {result.AdditionalInfo}",
                    AdditionalInfo = $"Stripe PaymentIntent: {request.PaymentIntentId}"
                };

                var savedTransaction = await Add(transactionDto);
                if (!savedTransaction.Success)
                {
                    return CreateErrorResult(500, "Failed to save payment transaction",
                        "Payment was captured but transaction record could not be saved");
                }

                // Set ProviderName for Stripe transactions (required for refunds);
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
                {
                    s.IsPaid = true;
                    s.PaymentMethodId = stripePaymentMethodId.Value;
                    s.UpdatedDate = DateTime.UtcNow;
                    s.UpdatedBy = _userService.GetLoggedInUser();
                });
                return new PaymentOrchestrationResult
                {
                    Success = true,
                    StatusCode = 200,
                    TransactionId = savedTransaction.Id,
                    TransactionReference = result.TransactionReference ?? result.TransactionId,
                    Status = result.Status.ToString(),
                    Message = result.Message ?? "Payment captured successfully"
                };
            }
            catch (Exception ex)
            {
                return CreateExceptionResult(ex, "capturing the Stripe payment");
            }
        }

        /// <summary>
        /// Process a real payment through payment gateway (Stripe, PayPal, etc.);
        /// </summary>
        public async Task<PaymentTransactionDto> ProcessPayment(Guid shipmentId, Guid paymentMethodId, decimal shippingRate, string? paymentMethodToken = null)
        {
            // Step 1: Validate inputs
            if (shipmentId == Guid.Empty)
                throw new ArgumentException("Shipment ID is required");
            if (paymentMethodId == Guid.Empty)
                throw new ArgumentException("Payment method ID is required");
            if (shippingRate <= 0)
                throw new ArgumentException("Shipping rate must be greater than zero");
            // Step 1.5: Idempotency guard - return existing non-failed transaction
            var idempotencyKey = $"ship:{shipmentId}:pm:{paymentMethodId}";

            var existingTransactions = await _repository.GetList(
                pt => pt.IdempotencyKey == idempotencyKey || (pt.ShipmentId == shipmentId && pt.PaymentMethodId == paymentMethodId),
                pt => pt,
                pt => pt.CreatedDate,
                true,
                pt => pt.PaymentMethod!,
                pt => pt.Shipment!);
            var existing = existingTransactions.FirstOrDefault(t => t.TransactionStatus != (int)PaymentTransactionStatus.Failed);
            if (existing != null)
            {
                var existingDto = _mapper.Map<PaymentTransactionDto>(existing);
                existingDto.TransactionStatusName = GetStatusName(existing.TransactionStatus);
                existingDto.PaymentMethodName = existing.PaymentMethod?.MethodEname ?? existing.PaymentMethod?.MethdAname;
                existingDto.ShipmentTrackingNumber = existing.Shipment?.TrackingNumber?.ToString();
                return existingDto;
            }

            // Step 2: Get payment method to calculate commission and get gateway
            var paymentMethod = await _paymentMethodRepository.GetById(paymentMethodId);
            if (paymentMethod == null)
                throw new ArgumentException($"Payment method with ID {paymentMethodId} not found");
            // Step 3: Calculate commission and total
            var commissionPercentage = paymentMethod.Commission ?? 0;
            var commissionAmount = shippingRate * (decimal)(commissionPercentage / 100);
            var totalAmount = shippingRate + commissionAmount;

            // Step 4: Get the appropriate payment gateway
            var gateway = await _paymentGatewayFactory.GetGatewayByIdAsync(paymentMethodId);
            // Step 5: Build payment request
            var paymentRequest = new PaymentRequest
            {
                Amount = totalAmount,
                Currency = "USD",
                Description = $"Shipment Payment - Shipment ID: {shipmentId}",
                Customer = new CustomerInfo
                {
                    UserId = _userService.GetLoggedInUser().ToString()
                },
                Metadata = new Dictionary<string, string>
                {
                    { "ShipmentId", shipmentId.ToString() },
                    { "PaymentMethodId", paymentMethodId.ToString() },
                    { "ShippingRate", shippingRate.ToString("F2") },
                    { "Commission", commissionAmount.ToString("F2") }
                },
                PaymentMethodToken = string.IsNullOrWhiteSpace(paymentMethodToken) ? null : paymentMethodToken,
                CaptureImmediately = true
            };

            // Step 6: Process payment through gateway
            var paymentResult = await gateway.ProcessPayment(paymentRequest);
            // Step 7: Create transaction record
            var transaction = new TbPaymentTransaction
            {
                IdempotencyKey = idempotencyKey,
                ProviderName = gateway.GetProviderName(),
                ProviderEventId = null,
                ShipmentId = shipmentId,
                PaymentMethodId = paymentMethodId,
                ShippingRate = shippingRate,
                CommissionPercentage = commissionPercentage,
                CommissionAmount = commissionAmount,
                TotalAmount = totalAmount,
                TransactionReference = paymentResult.TransactionId,
                TransactionStatus = MapPaymentStatusToInt(paymentResult.Status),
                ProcessedDate = paymentResult.Success ? paymentResult.ProcessedAt : (DateTime?)null,
                ErrorMessage = paymentResult.ErrorMessage,
                AdditionalInfo = paymentResult.AdditionalInfo,  // ✅ Store PayPal approval URL or other gateway info
                Notes = paymentResult.Success 
                    ? $"Payment processed successfully via {gateway.GetProviderName()}" 
                    : $"Payment failed via {gateway.GetProviderName()}: {paymentResult.ErrorMessage}",
                CreatedBy = _userService.GetLoggedInUser(),
                CreatedDate = DateTime.UtcNow,
                CurrentState = 1
            };

            // Step 8: Save to database
            await _repository.Add(transaction);
            // Step 9: Map to DTO and return
            var dto = _mapper.Map<PaymentTransactionDto>(transaction);
            dto.TransactionStatusName = GetStatusName(transaction.TransactionStatus);
            dto.PaymentMethodName = paymentMethod.MethodEname ?? paymentMethod.MethdAname;

            return dto;
        }

        /// <summary>
        /// Processes a full refund for a shipment's completed payment transaction.
        /// Supports PayPal, Stripe, and any other configured payment gateway.
        /// </summary>
        public async Task<PaymentTransactionDto> RefundPayment(Guid shipmentId, string reason)
        {
            // Validate shipment ID
            if (shipmentId == Guid.Empty)
                throw new ArgumentException("Shipment ID is required", nameof(shipmentId));
            // Get shipment
            var shipment = await _shipmentRepository.GetById(shipmentId);
            if (shipment == null)
                throw new ArgumentException($"Shipment with ID {shipmentId} not found");
            // Get payment transaction
            var transactions = await _repository.GetList(
                pt => pt.ShipmentId == shipmentId,
                pt => pt,
                pt => pt.CreatedDate,
                true,
                pt => pt.PaymentMethod!,
                pt => pt.Shipment!);
            var transaction = transactions.FirstOrDefault();
            if (transaction == null)
                throw new InvalidOperationException("No payment transaction found for this shipment");
            // Validate refund eligibility
            if (!shipment.IsPaid)
                throw new InvalidOperationException("Only paid shipments can be refunded");
            if (transaction.TransactionStatus == (int)PaymentTransactionStatus.Refunded)
                throw new InvalidOperationException("This payment has already been refunded");
            if (transaction.TransactionStatus != (int)PaymentTransactionStatus.Completed)
                throw new InvalidOperationException("Only completed payments can be refunded");
            if (string.IsNullOrWhiteSpace(transaction.TransactionReference))
                throw new InvalidOperationException("Missing transaction reference for refund");
            // Determine provider name (default to "PayPal" for backward compatibility);
            var providerName = !string.IsNullOrWhiteSpace(transaction.ProviderName) 
                ? transaction.ProviderName 
                : "PayPal"; // Legacy transactions may not have ProviderName set

            // Get appropriate payment gateway using provider name (not payment method name);
            var gateway = await _paymentGatewayFactory.GetGatewayByNameAsync(providerName);
            if (gateway == null)
                throw new InvalidOperationException($"{providerName} gateway is not configured");
            // Build refund request
            var refundRequest = new RefundRequest
            {
                TransactionId = transaction.TransactionReference,
                Amount = transaction.TotalAmount,
                Currency = "USD",
                Reason = string.IsNullOrWhiteSpace(reason) ? "Shipment refund" : reason,
                Type = RefundType.Full
            };

            // Process refund through gateway
            var refundResult = await gateway.ProcessRefund(refundRequest);
            if (!refundResult.Success)
                throw new InvalidOperationException(refundResult.ErrorMessage ?? $"{providerName} refund failed");
            // Update transaction record
            var refundTimestamp = DateTime.UtcNow;
            transaction.TransactionStatus = (int)PaymentTransactionStatus.Refunded;
            transaction.Notes = string.IsNullOrWhiteSpace(transaction.Notes)
                ? $"[REFUND - {refundTimestamp:yyyy-MM-dd HH:mm}]: {refundRequest.Reason}"
                : transaction.Notes + $"\n[REFUND - {refundTimestamp:yyyy-MM-dd HH:mm}]: {refundRequest.Reason}";
            transaction.AdditionalInfo = string.IsNullOrWhiteSpace(transaction.AdditionalInfo)
                ? $"{providerName} Refund ID: {refundResult.RefundId}"
                : transaction.AdditionalInfo + $" | {providerName} Refund ID: {refundResult.RefundId}";
            transaction.UpdatedBy = _userService.GetLoggedInUser();
            transaction.UpdatedDate = refundTimestamp;

            // Update transaction in database
            await _repository.Update(transaction);
            // Update shipment status to refunded
            await _shipmentRepository.UpdateFields(shipment.Id, s =>
            {
                s.IsPaid = false;
                s.CurrentState = (int)Business.Services.Shipment.ManageShipmentsState.ShipmentStatusEnum.Refunded;
                s.UpdatedBy = _userService.GetLoggedInUser();
                s.UpdatedDate = refundTimestamp;
            });
            var dto = _mapper.Map<PaymentTransactionDto>(transaction);
            dto.TransactionStatusName = GetStatusName(transaction.TransactionStatus);
            dto.PaymentMethodName = transaction.PaymentMethod?.MethodEname ?? transaction.PaymentMethod?.MethdAname;
            dto.ShipmentTrackingNumber = transaction.Shipment?.TrackingNumber?.ToString();
            return dto;
        }

        /// <summary>
        /// Map PaymentStatus enum from gateway to our integer status
        /// </summary>
        private int MapPaymentStatusToInt(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Completed => (int)PaymentTransactionStatus.Completed,
                PaymentStatus.Pending => (int)PaymentTransactionStatus.Pending,
                PaymentStatus.RequiresAction => (int)PaymentTransactionStatus.Pending,
                PaymentStatus.Failed => (int)PaymentTransactionStatus.Failed,
                PaymentStatus.Refunded => (int)PaymentTransactionStatus.Refunded,
                PaymentStatus.Canceled => (int)PaymentTransactionStatus.Failed,
                _ => (int)PaymentTransactionStatus.Failed
            };
        }

        /// <summary>
        /// Get payment transaction by shipment ID
        /// </summary>
        public async Task<PaymentTransactionDto?> GetByShipmentId(Guid shipmentId)
        {
            // Use GetList with filter instead of GetTableNoTracking
            var transactions = await _repository.GetList(
                pt => pt.ShipmentId == shipmentId,
                pt => pt,
                pt => pt.CreatedDate,
                true, // descending
                pt => pt.PaymentMethod!,
                pt => pt.Shipment!);
            var transaction = transactions.FirstOrDefault();
            if (transaction == null)
                return null;

            var dto = _mapper.Map<PaymentTransactionDto>(transaction);
            dto.TransactionStatusName = GetStatusName(transaction.TransactionStatus);
            dto.PaymentMethodName = transaction.PaymentMethod?.MethodEname ?? transaction.PaymentMethod?.MethdAname;
            dto.ShipmentTrackingNumber = transaction.Shipment?.TrackingNumber?.ToString();
            return dto;
        }

        /// <summary>
        /// Get all payment transactions for a user's shipments
        /// Educational: shows JOIN pattern for related data
        /// </summary>
        public async Task<PagedResult<PaymentTransactionDto>> GetUserPaymentHistory(int pageNumber = 1, int pageSize = 10, 
            Guid? userId = null)
        {
            // If userId not provided, filter will need to be handled by controller
            // For now, require userId to be passed from controller layer
            if (!userId.HasValue)
                throw new ArgumentException("User ID is required for payment history");
            var targetUserId = userId.Value;

            // Use GetList with filter for user's shipments
            var allTransactions = await _repository.GetList(
                pt => pt.Shipment != null && pt.Shipment.CreatedBy == targetUserId,
                pt => pt,
                pt => pt.CreatedDate,
                true, // descending
                pt => pt.PaymentMethod!,
                pt => pt.Shipment!);
            var transactionDtos = allTransactions.Select(t =>
            {
                var dto = _mapper.Map<PaymentTransactionDto>(t);
                dto.TransactionStatusName = GetStatusName(t.TransactionStatus);
                dto.PaymentMethodName = t.PaymentMethod?.MethodEname ?? t.PaymentMethod?.MethdAname;
                dto.ShipmentTrackingNumber = t.Shipment?.TrackingNumber?.ToString();
                return dto;
            }).ToList();
            // Apply paging
            var totalCount = transactionDtos.Count;
            var pagedItems = transactionDtos
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return new PagedResult<PaymentTransactionDto>
            {
                Items = pagedItems,
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        /// <summary>
        /// Get payment transaction by ID
        /// </summary>
        public async Task<PaymentTransactionDto?> GetByIdAsync(Guid id)
        {
            // Use GetList with filter to include navigation properties
            var transactions = await _repository.GetList(
                pt => pt.Id == id,
                pt => pt,
                pt => pt.CreatedDate,
                false,
                pt => pt.PaymentMethod!,
                pt => pt.Shipment!);
            var transaction = transactions.FirstOrDefault();
            if (transaction == null)
                return null;

            var dto = _mapper.Map<PaymentTransactionDto>(transaction);
            dto.TransactionStatusName = GetStatusName(transaction.TransactionStatus);
            dto.PaymentMethodName = transaction.PaymentMethod?.MethodEname ?? transaction.PaymentMethod?.MethdAname;
            dto.ShipmentTrackingNumber = transaction.Shipment?.TrackingNumber?.ToString();
            return dto;
        }

        /// <summary>
        /// Convert status enum to human-readable name
        /// </summary>
        private string GetStatusName(int status)
        {
            return status switch
            {
                0 => "Pending",
                1 => "Completed",
                2 => "Failed",
                3 => "Refunded",
                4 => "PartiallyRefunded",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get all payment transactions for admin with filtering
        /// </summary>
        public async Task<PagedResult<PaymentTransactionDto>> GetAllPaymentTransactions(
            int pageNumber = 1,
            int pageSize = 10,
            int? status = null,
            Guid? paymentMethodId = null,
            string? searchTerm = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            // Build filter expression
            System.Linq.Expressions.Expression<Func<TbPaymentTransaction, bool>> filter = pt => true;

            // Apply status filter
            if (status.HasValue)
            {
                var statusValue = status.Value;
                filter = filter.And(pt => pt.TransactionStatus == statusValue);
            }

            // Apply payment method filter
            if (paymentMethodId.HasValue)
            {
                var methodId = paymentMethodId.Value;
                filter = filter.And(pt => pt.PaymentMethodId == methodId);
            }

            // Apply search term filter (tracking number or transaction reference);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim();
                // Try to parse as tracking number (numeric);
                bool isNumericSearch = long.TryParse(search, out long trackingNumber);
                if (isNumericSearch)
                {
                    // Search by tracking number (exact or contains);
                    filter = filter.And(pt => 
                        (pt.Shipment != null && pt.Shipment.TrackingNumber == trackingNumber) ||
                        (pt.TransactionReference != null && pt.TransactionReference.ToLower().Contains(search.ToLower())));
                }
                else
                {
                    // Search only by transaction reference
                    var lowerSearch = search.ToLower();
                    filter = filter.And(pt => 
                        pt.TransactionReference != null && pt.TransactionReference.ToLower().Contains(lowerSearch));
                }
            }

            // Apply date range filters
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                filter = filter.And(pt => pt.CreatedDate >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1); // Include the entire end date
                filter = filter.And(pt => pt.CreatedDate < end);
            }

            // Get all transactions with filters
            var allTransactions = await _repository.GetList(
                filter,
                pt => pt,
                pt => pt.CreatedDate,
                true, // descending
                pt => pt.PaymentMethod!,
                pt => pt.Shipment!);
            // Map to DTOs
            var transactionDtos = allTransactions.Select(t =>
            {
                var dto = _mapper.Map<PaymentTransactionDto>(t);
                dto.TransactionStatusName = GetStatusName(t.TransactionStatus);
                dto.PaymentMethodName = t.PaymentMethod?.MethodEname ?? t.PaymentMethod?.MethdAname;
                dto.ShipmentTrackingNumber = t.Shipment?.TrackingNumber?.ToString();
                return dto;
            }).ToList();
            // Apply paging
            var totalCount = transactionDtos.Count;
            var pagedItems = transactionDtos
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return new PagedResult<PaymentTransactionDto>
            {
                Items = pagedItems,
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<bool> IsWebhookEventProcessed(string providerName, string providerEventId)
        {
            if (string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(providerEventId))
                return false;

            var webhookEvent = await _webhookEventRepository.GetFirstOrDefault(
                x => x.ProviderName == providerName && x.ProviderEventId == providerEventId);
            return webhookEvent != null && webhookEvent.IsProcessed;
        }

        public async Task RecordWebhookEvent(string providerName, string providerEventId, string? eventType, string? transactionReference, string payload)
        {
            if (string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(providerEventId))
                return;

            var existing = await _webhookEventRepository.GetFirstOrDefault(
                x => x.ProviderName == providerName && x.ProviderEventId == providerEventId);
            if (existing != null)
                return;

            var webhookEvent = new TbPaymentWebhookEvent
            {
                ProviderName = providerName,
                ProviderEventId = providerEventId,
                EventType = eventType,
                TransactionReference = transactionReference,
                Payload = payload,
                IsProcessed = false,
                ProcessingNotes = null,
                ReceivedAt = DateTime.UtcNow,
                CurrentState = 1,
                CreatedBy = _userService.GetLoggedInUser(),
                CreatedDate = DateTime.UtcNow
            };

            await _webhookEventRepository.Add(webhookEvent);
        }

        public async Task MarkWebhookEventProcessed(string providerName, string providerEventId, bool isProcessed, 
            string? processingNotes = null)
        {
            if (string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(providerEventId))
                return;

            var webhookEvent = await _webhookEventRepository.GetFirstOrDefault(
                x => x.ProviderName == providerName && x.ProviderEventId == providerEventId);
            if (webhookEvent == null)
                return;

            webhookEvent.IsProcessed = isProcessed;
            webhookEvent.ProcessingNotes = processingNotes;
            webhookEvent.UpdatedBy = _userService.GetLoggedInUser();
            webhookEvent.UpdatedDate = DateTime.UtcNow;

            await _webhookEventRepository.Update(webhookEvent);
        }

        public async Task<bool> ReconcileTransactionFromWebhook(string providerName, string providerEventId, 
            string? eventType, string? transactionReference, string payload)
        {
            if (string.IsNullOrWhiteSpace(transactionReference))
                return false;

            var txList = await _repository.GetList(
                pt => pt.TransactionReference == transactionReference,
                pt => pt,
                pt => pt.CreatedDate,
                true);
            var tx = txList.FirstOrDefault();
            if (tx == null)
                return false;

            var normalizedEvent = (eventType ?? string.Empty).ToLowerInvariant();
            if (normalizedEvent.Contains("succeeded") || normalizedEvent.Contains("completed") || normalizedEvent.Contains("captured"))
            {
                tx.TransactionStatus = (int)PaymentTransactionStatus.Completed;
                tx.ProcessedDate = DateTime.UtcNow;
                tx.ErrorMessage = null;
            }
            else if (normalizedEvent.Contains("failed") || normalizedEvent.Contains("denied") || normalizedEvent.Contains("canceled"))
            {
                tx.TransactionStatus = (int)PaymentTransactionStatus.Failed;
                tx.ErrorMessage = $"Webhook reconciliation marked as failed ({eventType})";
            }
            else if (normalizedEvent.Contains("refund"))
            {
                tx.TransactionStatus = (int)PaymentTransactionStatus.Refunded;
            }

            tx.ProviderName = providerName;
            tx.ProviderEventId = providerEventId;
            tx.Notes = string.IsNullOrWhiteSpace(tx.Notes)
                ? $"Reconciled by webhook: {eventType}"
                : tx.Notes + $"\nReconciled by webhook: {eventType}";
            tx.UpdatedBy = _userService.GetLoggedInUser();
            tx.UpdatedDate = DateTime.UtcNow;

            await _repository.Update(tx);
            return true;
        }

        public async Task ReconcileTransactionFromCallback(string paypalOrderId, Business.Payments.Gateway.PaymentResult captureResult)
        {
            // Find transaction by PayPal order ID (stored as TransactionReference during order creation);
            var transactions = await _repository.GetList(
                t => t.TransactionReference == paypalOrderId && t.ProviderName == "PayPal",
                t => t,
                t => t.CreatedDate,
                true);
            var transaction = transactions.FirstOrDefault();
            if (transaction == null)
            {
                throw new InvalidOperationException($"Payment transaction not found for PayPal order: {paypalOrderId}");
            }

            // Update transaction with capture details
            if (captureResult.Success)
            {
                transaction.TransactionStatus = (int)PaymentTransactionStatus.Completed;
                transaction.TransactionReference = captureResult.TransactionId;  // Update to capture ID
                transaction.ProcessedDate = captureResult.ProcessedAt;
                transaction.Notes = $"Payment captured successfully via PayPal. Capture ID: {captureResult.TransactionId}. {captureResult.AdditionalInfo}";
                transaction.ErrorMessage = null;
            }
            else
            {
                transaction.TransactionStatus = (int)PaymentTransactionStatus.Failed;
                transaction.ErrorMessage = captureResult.ErrorMessage;
                transaction.Notes = $"PayPal capture failed: {captureResult.ErrorMessage}";
            }

            transaction.UpdatedBy = _userService.GetLoggedInUser();
            transaction.UpdatedDate = DateTime.UtcNow;

            await _repository.Update(transaction);
        }

        public async Task<PaymentTransactionDto?> GetByTransactionReferenceAsync(string transactionReference)
        {
            if (string.IsNullOrWhiteSpace(transactionReference))
                return null;

            var transactions = await _repository.GetList(
                t => t.TransactionReference == transactionReference,
                t => t,
                t => t.CreatedDate,
                true);
            var transaction = transactions.FirstOrDefault();
            if (transaction == null)
                return null;

            return _mapper.Map<PaymentTransactionDto>(transaction);
        }
    }

    /// <summary>
    /// Extension method for combining LINQ expressions with AND logic
    /// </summary>
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> And<T>(
            this Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second)
        {
            var parameter = Expression.Parameter(typeof(T));
            var leftVisitor = new ReplaceExpressionVisitor(first.Parameters[0], parameter);
            var left = leftVisitor.Visit(first.Body);
            var rightVisitor = new ReplaceExpressionVisitor(second.Parameters[0], parameter);
            var right = rightVisitor.Visit(second.Body);
            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left!, right!), parameter);
        }

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression? Visit(Expression? node)
            {
                if (node == _oldValue)
                    return _newValue;
                return base.Visit(node);
            }
        }
    }
}
