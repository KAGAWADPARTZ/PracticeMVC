using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Models;
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

        [HttpGet]
        public async Task<IActionResult> GetFilteredTransactionHistory([FromQuery] TransactionHistoryFilter filter)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var historyResponse = await _transactionService.GetTransactionHistoryAsync(GetCurrentUserEmail()!, filter);
                return JsonSuccess(historyResponse);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving filtered transaction history");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionStatistics([FromQuery] TransactionHistoryFilter? filter = null)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var statistics = await _transactionService.GetTransactionStatisticsAsync(GetCurrentUserEmail()!, filter);
                return JsonSuccess(statistics);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving transaction statistics");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentTransactions([FromQuery] int count = 10)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var transactions = await _transactionService.GetRecentTransactionsAsync(GetCurrentUserEmail()!, count);
                return JsonSuccess(transactions);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving recent transactions");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(GetCurrentUserEmail()!, startDate, endDate);
                return JsonSuccess(transactions);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving transactions by date range");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionsByCategory([FromQuery] string category)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                if (string.IsNullOrEmpty(category))
                {
                    return JsonError("Category parameter is required");
                }

                var transactions = await _transactionService.GetTransactionsByCategoryAsync(GetCurrentUserEmail()!, category);
                return JsonSuccess(transactions);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving transactions by category");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchTransactions([FromQuery] string searchTerm)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                if (string.IsNullOrEmpty(searchTerm))
                {
                    return JsonError("Search term is required");
                }

                var transactions = await _transactionService.SearchTransactionsAsync(GetCurrentUserEmail()!, searchTerm);
                return JsonSuccess(transactions);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "searching transactions");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlySummary([FromQuery] int year)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var monthlySummary = await _transactionService.GetMonthlyTransactionSummaryAsync(GetCurrentUserEmail()!, year);
                return JsonSuccess(monthlySummary);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving monthly transaction summary");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionRequest request)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var result = await _transactionService.SaveUserTransactionAsync(GetCurrentUserEmail(), request);
                return Json(new { success = result.success, message = result.message });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "creating transaction");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(int id, [FromBody] Transaction transaction)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var success = await _transactionService.UpdateTransactionAsync(id, transaction);
                if (success)
                {
                    return JsonSuccess(null, "Transaction updated successfully");
                }
                return JsonError("Failed to update transaction");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "updating transaction");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(int id)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var success = await _transactionService.DeleteTransactionAsync(id);
                if (success)
                {
                    return JsonSuccess(null, "Transaction deleted successfully");
                }
                return JsonError("Failed to delete transaction");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "deleting transaction");
            }
        }
    }
}
