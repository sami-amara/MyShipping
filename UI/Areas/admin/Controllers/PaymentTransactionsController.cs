using AutoMapper;
using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI.Constants;
using UI.Helpers;
using AppResource;

namespace UI.Areas.admin.Controllers
{
    [Area("admin")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Reviewer},{RoleNames.Operation},{RoleNames.OperationManager}")]
    public class PaymentTransactionsController : Controller
    {
        private readonly ILogger<PaymentTransactionsController> _logger;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IPaymentMethods _paymentMethods;
        private readonly IMapper _mapper;

        public PaymentTransactionsController(
            ILogger<PaymentTransactionsController> logger,
            IPaymentTransactionService paymentTransactionService,
            IPaymentMethods paymentMethods,
            IMapper mapper)
        {
            _logger = logger;
            _paymentTransactionService = paymentTransactionService;
            _paymentMethods = paymentMethods;
            _mapper = mapper;
        }

        /// <summary>
        /// Admin: View all payment transactions with filtering
        /// </summary>
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 10,
            int? status = null,
            Guid? paymentMethodId = null,
            string? search = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                // Get all transactions with filters
                var transactions = await _paymentTransactionService.GetAllPaymentTransactions(
                    page,
                    pageSize,
                    status,
                    paymentMethodId,
                    search,
                    startDate,
                    endDate);

                var pager = transactions.ToPaginationInfo(windowSize: 7);

                // Get payment methods for filter dropdown
                var paymentMethods = await _paymentMethods.GetAll();

                ViewBag.Transactions = transactions;
                ViewBag.Pager = pager;
                ViewBag.PaymentMethods = paymentMethods;
                ViewBag.CurrentStatus = status;
                ViewBag.CurrentPaymentMethod = paymentMethodId;
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentStartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.CurrentEndDate = endDate?.ToString("yyyy-MM-dd");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load admin payment transaction list");

                // Return empty result on error
                var emptyResult = new PagedResult<PaymentTransactionDto>
                {
                    Items = new List<PaymentTransactionDto>(),
                    Page = 1,
                    PageSize = pageSize,
                    TotalCount = 0
                };

                ViewBag.Transactions = emptyResult;
                ViewBag.Pager = emptyResult.ToPaginationInfo();
                ViewBag.PaymentMethods = new List<PaymentMethodDto>();
                TempData["MessageType"] = 2; // Error
                TempData["Message"] = Labels.FailedToLoadPaymentTransactions;

                return View();
            }
        }

        /// <summary>
        /// Admin: View transaction details
        /// </summary>
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var transaction = await _paymentTransactionService.GetByIdAsync(id);

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction not found for ID: {TransactionId}", id);
                    TempData["MessageType"] = 2; // Error
                    TempData["Message"] = Labels.PaymentTransactionNotFound;
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Transaction = transaction;
                ViewBag.CanRefund = transaction.TransactionStatus == 1; // Completed status

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load transaction details for ID: {TransactionId}", id);
                TempData["MessageType"] = 2; // Error
                TempData["Message"] = Labels.FailedToLoadTransactionDetails;
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Admin: Process refund
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(Guid id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["MessageType"] = 2; // Error
                    TempData["Message"] = Labels.RefundReasonRequired;
                    return RedirectToAction(nameof(Details), new { id });
                }

                var originalTransaction = await _paymentTransactionService.GetByIdAsync(id);
                if (originalTransaction == null)
                {
                    TempData["MessageType"] = 2; // Error
                    TempData["Message"] = "Payment transaction not found";
                    return RedirectToAction(nameof(Index));
                }

                var refundedTransaction = await _paymentTransactionService.RefundPayment(originalTransaction.ShipmentId, reason);

                TempData["MessageType"] = 1; // Success
                TempData["Message"] = Labels.PaymentRefundSuccess;

                return RedirectToAction(nameof(Details), new { id = refundedTransaction.Id });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot refund transaction {TransactionId}", id);
                TempData["MessageType"] = 2; // Error
                TempData["Message"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process refund for transaction {TransactionId}", id);
                TempData["MessageType"] = 2; // Error
                TempData["Message"] = Labels.PaymentRefundFailed;
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
}
