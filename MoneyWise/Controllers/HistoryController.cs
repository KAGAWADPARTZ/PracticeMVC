using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Models;
using MoneyWise.Services;

namespace MoneyWise.Controllers
{
    public class HistoryController : BaseController
    {
        private readonly HistoryService _historyService;

        public HistoryController(HistoryService historyService, ILogger<HistoryController> logger, LoginService loginService)
            : base(logger, loginService)
        {
            _historyService = historyService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Validate session before proceeding
                var sessionValid = await ValidateSessionAsync();
                if (!sessionValid)
                {
                    return RedirectToAction("Index", "Login");
                }

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return RedirectToAction("Index", "Home");
                }

                var histories = await _historyService.GetUserHistoryAsync(userEmail);
                return View(histories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction history");
                TempData["ErrorMessage"] = "An error occurred while loading transaction history.";
                return View(new List<HistoryModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var histories = await _historyService.GetUserHistoryAsync(GetCurrentUserEmail()!);
                return JsonSuccess(histories);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving transaction history");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentHistory([FromQuery] int count = 10)
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var histories = await _historyService.GetRecentHistoryAsync(GetCurrentUserEmail()!, count);
                return JsonSuccess(histories);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving recent transaction history");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHistoryByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var histories = await _historyService.GetHistoryByDateRangeAsync(GetCurrentUserEmail()!, startDate, endDate);
                return JsonSuccess(histories);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving transaction history by date range");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHistorySummary()
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var summary = await _historyService.GetHistorySummaryAsync(GetCurrentUserEmail()!);
                return JsonSuccess(summary);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving transaction history summary");
            }
        }
    }
}

