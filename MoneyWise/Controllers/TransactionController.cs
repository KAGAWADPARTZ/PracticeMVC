using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Services;

namespace MoneyWise.Controllers
{
    public class TransactionController : BaseController
    {
        private readonly TransactionService _transactionService;

        public TransactionController(TransactionService transactionService, ILogger<TransactionController> logger)
            : base(logger)
        {
            _transactionService = transactionService;
        }

        public async Task<IActionResult> History()
        {
            try
            {
                var userEmail = GetCurrentUserEmail();
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
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var transactions = await _transactionService.GetUserTransactionsAsync(GetCurrentUserEmail()!);
                return JsonSuccess(transactions);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving transaction history");
            }
        }
    }
}
