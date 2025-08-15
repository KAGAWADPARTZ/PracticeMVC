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
        private readonly UserRepository _userRepository;
        private readonly SupabaseService _supabaseService;

        public HomeController(ILogger<HomeController> logger, LoginService loginService, SavingsService savingsService, SavingsCalculatorService calculatorService, UserRepository userRepository, SupabaseService supabaseService) 
            : base(logger, loginService)
        {
            _savingsService = savingsService;
            _calculatorService = calculatorService;
            _userRepository = userRepository;
            _supabaseService = supabaseService;
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
            var annualEarnings = savings?.Amount ?? 0; // Use current savings as annual earnings for now

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

        [HttpPost]
        public async Task<IActionResult> TestHistoryInsertion([FromBody] SavingsRequest request)
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var result = await _savingsService.TestHistoryInsertionAsync(GetCurrentUserEmail()!, request.SavingsAmount, request.Action);
                return Json(new { success = result, message = result ? "History insertion test successful" : "History insertion test failed" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "testing history insertion");
            }
        }

        [HttpGet]
        public IActionResult Debug()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TestDatabaseSchema()
        {
            try
            {
                var debugInfo = new List<string>();
                
                // Test 1: Get all users
                var users = await _userRepository.GetAllUsers();
                debugInfo.Add($"Found {users.Count} users");
                if (users.Any())
                {
                    debugInfo.Add($"Sample user: ID={users.First().UserID}, Email={users.First().Email}");
                }

                // Test 2: Get user savings
                var userEmail = GetCurrentUserEmail();
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var savings = await _savingsService.GetUserSavingsAsync(userEmail);
                    if (savings != null)
                    {
                        debugInfo.Add($"User savings: ID={savings.SavingsID}, Amount={savings.Amount}, UserID={savings.UserID}");
                    }
                    else
                    {
                        debugInfo.Add("No savings record found for user");
                    }
                }

                // Test 3: Get user history
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var histories = await _supabaseService.GetHistoriesByUserIdAsync(users.FirstOrDefault(u => u.Email == userEmail)?.UserID ?? 0);
                    debugInfo.Add($"Found {histories.Count} history records");
                    if (histories.Any())
                    {
                        var sample = histories.First();
                        debugInfo.Add($"Sample history: ID={sample.HistoryID}, Type={sample.Type}, Amount={sample.Amount}");
                    }
                }

                return Json(new { success = true, debugInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, debugInfo = new List<string> { $"Exception: {ex.Message}", $"StackTrace: {ex.StackTrace}" } });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestSavingsTableSchema()
        {
            try
            {
                var debugInfo = new List<string>();
                
                // Test 1: Try to get all savings records to see the actual schema
                var response = await _supabaseService.GetAllSavingsAsync();
                debugInfo.Add($"Savings table test completed. Found {response?.Count ?? 0} records");
                
                if (response != null && response.Any())
                {
                    var sample = response.First();
                    debugInfo.Add($"Sample savings record: {System.Text.Json.JsonSerializer.Serialize(sample)}");
                }

                return Json(new { success = true, debugInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, debugInfo = new List<string> { $"Exception: {ex.Message}", $"StackTrace: {ex.StackTrace}" } });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DebugInsertion([FromBody] SavingsRequest request)
        {
            try
            {
                var debugInfo = new List<string>();
                
                // Step 1: Check session
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) 
                {
                    debugInfo.Add("Session validation failed");
                    return Json(new { success = false, debugInfo });
                }
                debugInfo.Add("Session validation passed");

                // Step 2: Check authentication
                var authResult = ValidateAuthentication();
                if (authResult != null) 
                {
                    debugInfo.Add("Authentication failed");
                    return Json(new { success = false, debugInfo });
                }
                debugInfo.Add("Authentication passed");

                // Step 3: Get user email
                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    debugInfo.Add("User email is null or empty");
                    return Json(new { success = false, debugInfo });
                }
                debugInfo.Add($"User email: {userEmail}");

                // Step 4: Get user from database
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                if (user == null)
                {
                    debugInfo.Add("User not found in database");
                    return Json(new { success = false, debugInfo });
                }
                debugInfo.Add($"User found: ID={user.UserID}");

                // Step 5: Test history insertion
                var history = new HistoryModel
                {
                    HistoryID = 0,
                    UserID = user.UserID,
                    Type = request.Action,
                    Amount = (float)request.SavingsAmount,
                    created_at = DateTime.UtcNow
                };
                debugInfo.Add($"History record created: {System.Text.Json.JsonSerializer.Serialize(history)}");

                var result = await _supabaseService.CreateHistoryAsync(history);
                debugInfo.Add($"History insertion result: {result}");

                return Json(new { success = result, debugInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, debugInfo = new List<string> { $"Exception: {ex.Message}", $"StackTrace: {ex.StackTrace}" } });
            }
        }
    }
}

