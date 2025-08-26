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
        private readonly SavingsService _savingsService;

        public BudgetController(BudgetRulesService budgetRulesService, SupabaseService supabaseService, SavingsService savingsService, ILogger<BudgetController> logger, LoginService loginService)
            : base(logger, loginService)
        {
            _budgetRulesService = budgetRulesService;
            _supabaseService = supabaseService;
            _savingsService = savingsService;
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

                // Get user ID first
                var userId = await GetUserIdFromEmailAsync(userEmail);
                if (userId == 0L)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return View(new List<BudgetRulesModel>());
                }

                // Get budget rules from service (percentages only)
                var budgets = await _budgetRulesService.GetBudgetRulesByUserAsync(userId);
                
                // Convert BudgetRules to BudgetRulesModel for the view
                var budgetModels = budgets.Select(b => new BudgetRulesModel
                {
                    UserID = b.UserID,
                    Savings = b.Savings,      // Percentage
                    Needs = b.Needs,          // Percentage
                    Wants = b.Wants,          // Percentage
                    updated_at = b.updated_at
                }).ToList();
                
                // Get user's total savings amount
                var userSavings = await _savingsService.GetUserSavingsAsync(userEmail);
                var totalSavingsAmount = userSavings?.Amount ?? 0;
                
                // Calculate actual amounts based on percentages
                if (budgetModels.Any())
                {
                    var latestBudget = budgetModels.First();
                    var savingsAmount = (totalSavingsAmount * latestBudget.Savings) / 100;
                    var needsAmount = (totalSavingsAmount * latestBudget.Needs) / 100;
                    var wantsAmount = (totalSavingsAmount * latestBudget.Wants) / 100;
                    
                    ViewBag.TotalIncome = totalSavingsAmount.ToString("F2");
                    ViewBag.TotalSavings = savingsAmount.ToString("F2");
                    ViewBag.TotalNeeds = needsAmount.ToString("F2");
                    ViewBag.TotalWants = wantsAmount.ToString("F2");
                }
                else
                {
                    ViewBag.TotalIncome = "0.00";
                    ViewBag.TotalSavings = "0.00";
                    ViewBag.TotalNeeds = "0.00";
                    ViewBag.TotalWants = "0.00";
                }
                
                return View(budgetModels);
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

                // Validate that percentages add up to 100%
                if (budgetRule.Savings + budgetRule.Needs + budgetRule.Wants != 100)
                {
                    return JsonError("Savings, Needs, and Wants percentages must add up to 100%");
                }

                // Get user ID from email
                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return JsonError("User email not found");
                }
                
                budgetRule.UserID = await GetUserIdFromEmailAsync(userEmail);
                budgetRule.updated_at = DateTime.UtcNow;

                // Convert BudgetRulesModel to BudgetRules (percentages only)
                var budgetRuleEntity = new BudgetRules
                {
                    UserID = budgetRule.UserID,
                    Savings = budgetRule.Savings,      // Percentage
                    Needs = budgetRule.Needs,          // Percentage
                    Wants = budgetRule.Wants,          // Percentage
                    updated_at = budgetRule.updated_at
                };

                var createdBudgetRule = await _budgetRulesService.UpsertBudgetRuleAsync(budgetRuleEntity);
                if (createdBudgetRule == null)
                {
                    return JsonError("Failed to create budget rule");
                }

                // Convert back to model for response
                var responseModel = new BudgetRulesModel
                {
                    UserID = createdBudgetRule.UserID,
                    Savings = createdBudgetRule.Savings,
                    Needs = createdBudgetRule.Needs,
                    Wants = createdBudgetRule.Wants,
                    updated_at = createdBudgetRule.updated_at
                };

                return JsonSuccess(responseModel, "Budget rule created successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "creating budget rule");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateBudgetRule(long userId, [FromBody] BudgetRulesModel budgetRule)
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

                // Validate that percentages add up to 100%
                if (budgetRule.Savings + budgetRule.Needs + budgetRule.Wants != 100)
                {
                    return JsonError("Savings, Needs, and Wants percentages must add up to 100%");
                }

                budgetRule.UserID = userId;
                budgetRule.updated_at = DateTime.UtcNow;

                // Convert BudgetRulesModel to BudgetRules (percentages only)
                var budgetRuleEntity = new BudgetRules
                {
                    UserID = budgetRule.UserID,
                    Savings = budgetRule.Savings,      // Percentage
                    Needs = budgetRule.Needs,          // Percentage
                    Wants = budgetRule.Wants,          // Percentage
                    updated_at = budgetRule.updated_at
                };

                var updatedBudgetRule = await _budgetRulesService.UpdateBudgetRuleAsync(budgetRuleEntity);
                if (updatedBudgetRule == null)
                {
                    return JsonError("Budget rule not found or you don't have permission to update it");
                }

                // Convert back to model for response
                var responseModel = new BudgetRulesModel
                {
                    UserID = updatedBudgetRule.UserID,
                    Savings = updatedBudgetRule.Savings,
                    Needs = updatedBudgetRule.Needs,
                    Wants = updatedBudgetRule.Wants,
                    updated_at = updatedBudgetRule.updated_at
                };

                return JsonSuccess(responseModel, "Budget rule updated successfully");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "updating budget rule");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteBudgetRule(long userId)
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

                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                {
                    return JsonError("User email not found");
                }

                var userId = await GetUserIdFromEmailAsync(userEmail);
                if (userId == 0L)
                {
                    return JsonError("User not found");
                }

                // Get user's total savings amount
                var userSavings = await _savingsService.GetUserSavingsAsync(userEmail);
                var totalSavingsAmount = userSavings?.Amount ?? 0;

                // Get budget rules (percentages)
                var budgetRules = await _budgetRulesService.GetBudgetRulesByUserAsync(userId);
                var summary = new Dictionary<string, object>();

                if (budgetRules.Any())
                {
                    var latestRule = budgetRules.OrderByDescending(b => b.updated_at).First();
                    summary["TotalIncome"] = totalSavingsAmount;
                    summary["TotalSavings"] = (totalSavingsAmount * latestRule.Savings) / 100;
                    summary["TotalNeeds"] = (totalSavingsAmount * latestRule.Needs) / 100;
                    summary["TotalWants"] = (totalSavingsAmount * latestRule.Wants) / 100;
                    summary["SavingsPercentage"] = latestRule.Savings;
                    summary["NeedsPercentage"] = latestRule.Needs;
                    summary["WantsPercentage"] = latestRule.Wants;
                }
                else
                {
                    summary["TotalIncome"] = totalSavingsAmount;
                    summary["TotalSavings"] = 0;
                    summary["TotalNeeds"] = 0;
                    summary["TotalWants"] = 0;
                    summary["SavingsPercentage"] = 0;
                    summary["NeedsPercentage"] = 0;
                    summary["WantsPercentage"] = 0;
                }

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

                // Convert to model for response
                var responseModel = new BudgetRulesModel
                {
                    UserID = budgetRule.UserID,
                    Savings = budgetRule.Savings,
                    Needs = budgetRule.Needs,
                    Wants = budgetRule.Wants,
                    updated_at = budgetRule.updated_at
                };

                return JsonSuccess(responseModel);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving budget rule");
            }
        }

        private async Task<long> GetUserIdFromEmailAsync(string email)
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

        [HttpGet]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var connectionTest = await _budgetRulesService.TestDatabaseConnectionAsync();
                
                if (connectionTest)
                {
                    return JsonSuccess("Database connection test successful");
                }
                else
                {
                    return JsonError("Database connection test failed");
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "testing database connection");
            }
        }
    }
}
