using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Models;
using MoneyWise.Services;

namespace MoneyWise.Controllers
{
    public class HomeController : BaseController
    {
        private readonly SavingsService _savingsService;
        private readonly SavingsCalculatorService _calculatorService;

        public HomeController(ILogger<HomeController> logger, SavingsService savingsService, SavingsCalculatorService calculatorService) 
            : base(logger)
        {
            _savingsService = savingsService;
            _calculatorService = calculatorService;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = GetCurrentUserEmail();

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
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;
               
                var savings = await _savingsService.GetUserSavingsAsync(GetCurrentUserEmail()!);
                
                if (savings != null)
                {
                    return JsonSuccess(new { savingsAmount = savings.Amount });
                }
                
                return JsonSuccess(new { savingsAmount = 0, savingsGoal = "" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "getting savings");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSavings([FromBody] SavingsRequest request)
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                _logger.LogInformation("User email from claims: {UserEmail}", GetCurrentUserEmail());
                
                var result = await _savingsService.SaveUserSavingsAsync(GetCurrentUserEmail()!, request);
                
                _logger.LogInformation("SaveUserSavingsAsync result: {Success}, {Message}", result.success, result.message);
                return Json(new { success = result.success, message = result.message });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "updating savings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyEarnings()
        {
            try
            {
                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var data = await _calculatorService.GetMonthlyEarningsAsync(GetCurrentUserEmail()!);
                return JsonSuccess(data);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "getting monthly earnings");
            }
        }
    }
}

