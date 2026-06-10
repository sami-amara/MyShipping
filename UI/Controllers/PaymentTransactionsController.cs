using AutoMapper;
using Business.Contracts;
using Business.DTOS;
using DataAccessLayer.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UI.Helpers;

namespace UI.Controllers
{
    [Authorize]
    public class PaymentTransactionsController : Controller
    {
        private readonly ILogger<PaymentTransactionsController> _logger;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly IMapper _mapper;

        public PaymentTransactionsController(
            ILogger<PaymentTransactionsController> logger,
            IPaymentTransactionService paymentTransactionService,
            IMapper mapper)
        {
            _logger = logger;
            _paymentTransactionService = paymentTransactionService;
            _mapper = mapper;
        }

        /// <summary>
        /// Display user's payment transaction history
        /// </summary>
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Unable to retrieve user ID from claims");
                    TempData["MessageType"] = 2; // Error
                    return RedirectToAction("Index", "Home");
                }

                // Get current user's payment history
                var transactions = await _paymentTransactionService.GetUserPaymentHistory(page, pageSize, userId);

                var pager = transactions.ToPaginationInfo(windowSize: 7);

                ViewBag.Transactions = transactions;
                ViewBag.Pager = pager;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load payment transaction history");

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

                TempData["MessageType"] = 2; // Error
                return View();
            }
        }

        /// <summary>
        /// Display detailed receipt for a specific transaction
        /// </summary>
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var transaction = await _paymentTransactionService.GetByIdAsync(id);

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction not found for ID: {TransactionId}", id);
                    ViewBag.Transaction = null;
                    return View();
                }

                ViewBag.Transaction = transaction;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load payment transaction details for ID: {TransactionId}", id);
                ViewBag.Transaction = null;
                return View();
            }
        }
    }
}
