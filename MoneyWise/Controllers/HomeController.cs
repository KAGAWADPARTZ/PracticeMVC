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

        public HomeController(ILogger<HomeController> logger, SavingsService savingsService)
        {
            _logger = logger;
            _savingsService = savingsService;
        }

        public IActionResult Index()
        {
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
    }

        // [HttpPost]
        // public async Task<IActionResult> SaveTransaction([FromBody] TransactionRequest request)
        // {
        //     var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        //     var result = await _transactionService.SaveUserTransactionAsync(userEmail, request);
            
        //     return Json(new { success = result.success, message = result.message });
        // }

        // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        // public IActionResult Error()
        // {
        //     return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        // }
}

