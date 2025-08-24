using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Models;
using MoneyWise.Services;

namespace MoneyWise.Controllers
{
    public class BudgetRulesController : BaseController
    {
        private readonly BudgetRulesService _budgetRulesService;
        private readonly UserRepository _userRepository;

        public BudgetRulesController(BudgetRulesService budgetRulesService, UserRepository userRepository, ILogger<BudgetRulesController> logger, LoginService loginService)
            : base(logger, loginService)
        {
            _budgetRulesService = budgetRulesService;
            _userRepository = userRepository;
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

                // Get user from database
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index", "Home");
                }

                var budgetRules = await _budgetRulesService.GetBudgetRulesByUserAsync(user.UserID);
                return View(budgetRules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving budget rules");
                TempData["ErrorMessage"] = "An error occurred while loading budget rules.";
                return View(new List<BudgetRules>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBudgetRules()
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
                    return JsonError("User not authenticated");
                }

                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return JsonError("User not found");
                }

                var budgetRules = await _budgetRulesService.GetBudgetRulesByUserAsync(user.UserID);
                return JsonSuccess(budgetRules);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving budget rules");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBudgetRuleById(int id)
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var budgetRule = await _budgetRulesService.GetBudgetRuleByIdAsync(id);
                
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
                    return JsonError("User not authenticated");
                }

                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return JsonError("User not found");
                }

                var summary = await _budgetRulesService.GetBudgetSummaryAsync(user.UserID);
                return JsonSuccess(summary);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving budget summary");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBudgetRule([FromBody] BudgetRules budgetRule)
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
                    return JsonError("User not authenticated");
                }

                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return JsonError("User not found");
                }

                // Set the user ID from the authenticated user
                budgetRule.UserID = user.UserID;

                // Validate budget percentages
                var isValidPercentages = await _budgetRulesService.ValidateBudgetPercentagesAsync(
                    budgetRule.SavingsAmount, 
                    budgetRule.NeedsAmount, 
                    budgetRule.WantsAmount
                );

                if (!isValidPercentages)
                {
                    return JsonError("Budget percentages must add up to 100%. Current total: " + 
                        (budgetRule.SavingsAmount + budgetRule.NeedsAmount + budgetRule.WantsAmount) + "%");
                }

                var createdBudgetRule = await _budgetRulesService.CreateBudgetRuleAsync(budgetRule);
                
                if (createdBudgetRule != null)
                {
                    return JsonSuccess(createdBudgetRule, "Budget rule created successfully");
                }
                else
                {
                    return JsonError("Failed to create budget rule");
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "creating budget rule");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateBudgetRule([FromBody] BudgetRules budgetRule)
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
                    return JsonError("User not authenticated");
                }

                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return JsonError("User not found");
                }

                // Verify the budget rule belongs to the authenticated user
                var existingRule = await _budgetRulesService.GetBudgetRuleByIdAsync(budgetRule.BudgetRulesID);
                if (existingRule == null || existingRule.UserID != user.UserID)
                {
                    return JsonError("Budget rule not found or access denied");
                }

                // Set the user ID from the authenticated user
                budgetRule.UserID = user.UserID;

                // Validate budget percentages
                var isValidPercentages = await _budgetRulesService.ValidateBudgetPercentagesAsync(
                    budgetRule.SavingsAmount, 
                    budgetRule.NeedsAmount, 
                    budgetRule.WantsAmount
                );

                if (!isValidPercentages)
                {
                    return JsonError("Budget percentages must add up to 100%. Current total: " + 
                        (budgetRule.SavingsAmount + budgetRule.NeedsAmount + budgetRule.WantsAmount) + "%");
                }

                var updatedBudgetRule = await _budgetRulesService.UpdateBudgetRuleAsync(budgetRule);
                
                if (updatedBudgetRule != null)
                {
                    return JsonSuccess(updatedBudgetRule, "Budget rule updated successfully");
                }
                else
                {
                    return JsonError("Failed to update budget rule");
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "updating budget rule");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBudgetRule(int id)
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
                    return JsonError("User not authenticated");
                }

                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return JsonError("User not found");
                }

                // Verify the budget rule belongs to the authenticated user
                var existingRule = await _budgetRulesService.GetBudgetRuleByIdAsync(id);
                if (existingRule == null || existingRule.UserID != user.UserID)
                {
                    return JsonError("Budget rule not found or access denied");
                }

                var success = await _budgetRulesService.DeleteBudgetRuleAsync(id);
                
                if (success)
                {
                    return JsonSuccess(new { }, "Budget rule deleted successfully");
                }
                else
                {
                    return JsonError("Failed to delete budget rule");
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "deleting budget rule");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpsertBudgetRule([FromBody] BudgetRules budgetRule)
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
                    return JsonError("User not authenticated");
                }

                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return JsonError("User not found");
                }

                // Set the user ID from the authenticated user
                budgetRule.UserID = user.UserID;

                // Validate budget percentages
                var isValidPercentages = await _budgetRulesService.ValidateBudgetPercentagesAsync(
                    budgetRule.SavingsAmount, 
                    budgetRule.NeedsAmount, 
                    budgetRule.WantsAmount
                );

                if (!isValidPercentages)
                {
                    return JsonError("Budget percentages must add up to 100%. Current total: " + 
                        (budgetRule.SavingsAmount + budgetRule.NeedsAmount + budgetRule.WantsAmount) + "%");
                }

                var upsertedBudgetRule = await _budgetRulesService.UpsertBudgetRuleAsync(budgetRule);
                
                if (upsertedBudgetRule != null)
                {
                    var action = budgetRule.BudgetRulesID > 0 ? "updated" : "created";
                    return JsonSuccess(upsertedBudgetRule, $"Budget rule {action} successfully");
                }
                else
                {
                    return JsonError("Failed to upsert budget rule");
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "upserting budget rule");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ValidateBudgetPercentages(int savings, int needs, int wants)
        {
            try
            {
                var sessionResponse = await ValidateSessionAndReturnResponseAsync();
                if (sessionResponse != null) return sessionResponse;

                var authResult = ValidateAuthentication();
                if (authResult != null) return authResult;

                var isValid = await _budgetRulesService.ValidateBudgetPercentagesAsync(savings, needs, wants);
                var total = savings + needs + wants;
                
                return JsonSuccess(new { 
                    isValid, 
                    total, 
                    message = isValid ? "Budget percentages are valid" : "Budget percentages must add up to 100%" 
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "validating budget percentages");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentBudgetRules(int count = 5)
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
                    return JsonError("User not authenticated");
                }

                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return JsonError("User not found");
                }

                var budgetRules = await _budgetRulesService.GetBudgetRulesByUserAsync(user.UserID);
                var recentRules = budgetRules.OrderByDescending(b => b.updated_at).Take(count).ToList();
                
                return JsonSuccess(recentRules);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving recent budget rules");
            }
        }
    }
}
