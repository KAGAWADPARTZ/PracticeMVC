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

        public HomeController(ILogger<HomeController> logger, LoginService loginService, SavingsService savingsService, SavingsCalculatorService calculatorService) 
            : base(logger, loginService)
        {
            _savingsService = savingsService;
            _calculatorService = calculatorService;
        }

        public async Task<IActionResult> Index()
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
                ViewBag.MonthlyEarnings = 0;
                ViewBag.AnnualEarnings = 0;
                return View();
            }

            var savings = await _savingsService.GetUserSavingsAsync(userEmail);
            var annualEarnings = await _calculatorService.CalculateAnnualEarningsAsync(userEmail);

            ViewBag.MonthlyEarnings = savings?.Amount ?? 0;
            ViewBag.AnnualEarnings = annualEarnings;

            // Add session info to ViewBag for display
            ViewBag.RemainingSessionTime = GetRemainingSessionTime();
            ViewBag.IsSessionExpiringSoon = IsSessionExpiringSoon();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSavings()
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

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
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

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
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

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

        [HttpGet]
        public async Task<IActionResult> GetSessionInfo()
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var remainingTime = GetRemainingSessionTime();
                var isExpiringSoon = IsSessionExpiringSoon();

                return JsonSuccess(new 
                { 
                    remainingTime = remainingTime.TotalMinutes,
                    isExpiringSoon = isExpiringSoon,
                    formattedTime = $"{remainingTime.Hours:D2}:{remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}"
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "getting session info");
            }
        }
    }
}

