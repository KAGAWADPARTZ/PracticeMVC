using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Models;
using MoneyWise.Services;

namespace MoneyWise.Controllers
{
    public class BudgetController : BaseController
    {
        private readonly BudgetRulesService _budgetRulesService;
        private readonly SupabaseService _supabaseService;

        public BudgetController(BudgetRulesService budgetRulesService, SupabaseService supabaseService, ILogger<BudgetController> logger, LoginService loginService)
            : base(logger, loginService)
        {
            _budgetRulesService = budgetRulesService;
            _supabaseService = supabaseService;
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

                // Get budget rules from service
                var budgets = await _budgetRulesService.GetUserBudgetRulesAsync(userEmail);
                
                // Get budget summary
                var summary = await _budgetRulesService.GetBudgetSummaryAsync(userEmail);
                
                // Set ViewBag values for display
                ViewBag.TotalIncome = summary["TotalIncome"].ToString();
                ViewBag.TotalSavings = summary["TotalSavings"].ToString();
                ViewBag.TotalNeeds = summary["TotalNeeds"].ToString();
                ViewBag.TotalWants = summary["TotalWants"].ToString();
                
                return View(budgets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving budget data");
                TempData["ErrorMessage"] = "An error occurred while loading budget data.";
                return View(new List<BudgetRulesModel>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBudgetRule([FromBody] BudgetRulesModel budgetRule)
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                if (!ModelState.IsValid)
                {
                    return JsonError("Invalid budget rule data", ModelState);
                }

                // Get user ID from email
                var userEmail = GetCurrentUserEmail()!;
                budgetRule.UserID = await GetUserIdFromEmailAsync(userEmail);
                budgetRule.updated_at = DateTime.UtcNow;

                var createdBudgetRule = await _budgetRulesService.UpsertBudgetRuleAsync(budgetRule);
                return JsonSuccess(createdBudgetRule, "Budget rule created successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "creating budget rule");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateBudgetRule(int userId, [FromBody] BudgetRulesModel budgetRule)
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                if (!ModelState.IsValid)
                {
                    return JsonError("Invalid budget rule data", ModelState);
                }

                budgetRule.UserID = userId;
                budgetRule.updated_at = DateTime.UtcNow;

                var updatedBudgetRule = await _budgetRulesService.UpdateBudgetRuleAsync(budgetRule);
                if (updatedBudgetRule == null)
                {
                    return JsonError("Budget rule not found or you don't have permission to update it");
                }

                return JsonSuccess(updatedBudgetRule, "Budget rule updated successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "updating budget rule");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteBudgetRule(int userId)
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var success = await _budgetRulesService.DeleteBudgetRuleAsync(userId);
                if (!success)
                {
                    return JsonError("Budget rule not found or you don't have permission to delete it");
                }

                return JsonSuccess(null, "Budget rule deleted successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "deleting budget rule");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBudgetSummary()
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var summary = await _budgetRulesService.GetBudgetSummaryAsync(GetCurrentUserEmail()!);
                return JsonSuccess(summary);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving budget summary");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBudgetRuleById(int userId)
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var budgetRule = await _budgetRulesService.GetBudgetRuleByIdAsync(userId);
                if (budgetRule == null)
                {
                    return JsonError("Budget rule not found");
                }

                return JsonSuccess(budgetRule);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving budget rule");
            }
        }

        private async Task<int> GetUserIdFromEmailAsync(string email)
        {
            try
            {
                var user = await _supabaseService.GetUserByEmailAsync(email);
                return user?.UserID ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID for email: {Email}", email);
                return 0;
            }
        }
    }
}
