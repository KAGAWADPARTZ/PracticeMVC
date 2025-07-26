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
        private readonly TransactionService _transactionService;

        public HomeController(ILogger<HomeController> logger, TransactionService transactionService)
        {
            _logger = logger;
            _transactionService = transactionService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveTransaction([FromBody] TransactionRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var result = await _transactionService.SaveUserTransactionAsync(userEmail, request);
            
            return Json(new { success = result.success, message = result.message });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
