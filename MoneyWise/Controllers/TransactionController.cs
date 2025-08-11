using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Services;
using System.Security.Claims;

namespace MoneyWise.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly TransactionService _transactionService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(TransactionService transactionService, ILogger<TransactionController> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        public async Task<IActionResult> History()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return RedirectToAction("Index", "Home");
                }

                var transactions = await _transactionService.GetUserTransactionsAsync(userEmail);
                return View(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction history");
                TempData["ErrorMessage"] = "An error occurred while loading transaction history.";
                return View(new List<Models.Transaction>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionHistory()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var transactions = await _transactionService.GetUserTransactionsAsync(userEmail);
                return Json(new { success = true, transactions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction history");
                return Json(new { success = false, message = "An error occurred while retrieving transaction history" });
            }
        }
    }
}
