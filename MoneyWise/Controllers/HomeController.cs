using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Models;
using MoneyWise.Services;
using System.Security.Claims;

namespace MoneyWise.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        // private readonly TransactionService _transactionService;
        private readonly SavingsService _savingsService;
        private readonly SavingsCalculatorService _calculatorService;

        public HomeController(ILogger<HomeController> logger, SavingsService savingsService, SavingsCalculatorService calculatorService)
        {
            _logger = logger;
            _savingsService = savingsService;
            _calculatorService = calculatorService;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                ViewBag.MonthlyEarnings = 0;
                ViewBag.AnnualEarnings = 0;
                return View();
            }

            var savings = await _savingsService.GetUserSavingsAsync(userEmail);
            var annualEarnings = await _calculatorService.CalculateAnnualEarningsAsync(userEmail);

            ViewBag.MonthlyEarnings = savings?.Amount ?? 0;
            ViewBag.AnnualEarnings = annualEarnings;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSavings()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }
               
                var savings = await _savingsService.GetUserSavingsAsync(userEmail);
                
                if (savings != null)
                {
                    return Json(new { 
                        success = true, 
                        savingsAmount = savings.Amount,
                    });
                }
                else
                {
                    return Json(new { 
                        success = true, 
                        savingsAmount = 0,
                        savingsGoal = ""
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting savings");
                return Json(new { success = false, message = "An error occurred while getting savings" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSavings([FromBody] SavingsRequest request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                 _logger.LogInformation("User email from claims: {UserEmail}", userEmail);
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _savingsService.SaveUserSavingsAsync(userEmail, request);
                
                 _logger.LogInformation("SaveUserSavingsAsync result: {Success}, {Message}", result.success, result.message);
                return Json(new { success = result.success, message = result.message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating savings");
                return Json(new { success = false, message = "An error occurred while updating savings" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyEarnings()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return Json(new { success = false, message = "Unauthorized" });

            var data = await _calculatorService.GetMonthlyEarningsAsync(userEmail);
            return Json(new { success = true, data });
        }

    }

}

