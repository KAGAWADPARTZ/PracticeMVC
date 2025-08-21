using MoneyWise.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MoneyWise.Services
{
    public class BudgetRulesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseApiKey;
        private readonly ILogger<BudgetRulesService> _logger;

        public BudgetRulesService(IConfiguration configuration, ILogger<BudgetRulesService> logger)
        {
            _supabaseUrl = configuration["Authentication:Supabase:Url"]!;
            _supabaseApiKey = configuration["Authentication:Supabase:ApiKey"]!;
            _logger = logger;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_supabaseUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<BudgetRulesModel>> GetUserBudgetRulesAsync(string userEmail)
        {
            try
            {
                _logger.LogInformation("Looking for budget rules for user: {UserEmail}", userEmail);
                
                // First get the user ID from email
                var userResponse = await _httpClient.GetAsync($"/rest/v1/Users?Email=eq.{Uri.EscapeDataString(userEmail)}&select=UserID");
                if (!userResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get user ID for email: {UserEmail}", userEmail);
                    return new List<BudgetRulesModel>();
                }

                var userJson = await userResponse.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<Users>>(userJson);
                var user = users?.FirstOrDefault();

                if (user == null)
                {
                    _logger.LogWarning("User not found for email: {UserEmail}", userEmail);
                    return new List<BudgetRulesModel>();
                }

                // Now get budget rules for the user
                var response = await _httpClient.GetAsync($"/rest/v1/BudgetRules?UserID=eq.{user.UserID}&select=*");
                _logger.LogInformation("Budget rules response status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get budget rules. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                    return new List<BudgetRulesModel>();
                }

                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Budget rules response: {Json}", json);

                var budgetRules = JsonSerializer.Deserialize<List<BudgetRulesModel>>(json);
                return budgetRules ?? new List<BudgetRulesModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving budget rules for user {UserEmail}", userEmail);
                return new List<BudgetRulesModel>();
            }
        }

        public async Task<BudgetRulesModel?> GetBudgetRuleByIdAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/rest/v1/BudgetRules?UserID=eq.{userId}&select=*&limit=1");
                _logger.LogInformation("GetBudgetRuleByIdAsync response status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get budget rule. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var budgetRules = JsonSerializer.Deserialize<List<BudgetRulesModel>>(json);
                return budgetRules?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving budget rule for UserID {UserID}", userId);
                return null;
            }
        }

        public async Task<BudgetRulesModel> CreateBudgetRuleAsync(BudgetRulesModel budgetRule)
        {
            try
            {
                // Create a custom JSON object that matches Supabase expectations
                var budgetData = new
                {
                    UserID = budgetRule.UserID,
                    Savings = budgetRule.Savings,
                    Needs = budgetRule.Needs,
                    Wants = budgetRule.Wants,
                    Amount = budgetRule.Amount,
                    updated_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var json = JsonSerializer.Serialize(budgetData);
                _logger.LogInformation("JSON being sent to BudgetRules table: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/rest/v1/BudgetRules", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("BudgetRules create response status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("BudgetRules create response body: {Content}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create budget rule. Status: {StatusCode}, Content: {Content}", response.StatusCode, responseContent);
                    throw new Exception($"Failed to create budget rule: {responseContent}");
                }

                budgetRule.updated_at = DateTime.UtcNow;
                return budgetRule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating budget rule for UserID {UserID}", budgetRule.UserID);
                throw;
            }
        }

        public async Task<BudgetRulesModel?> UpdateBudgetRuleAsync(BudgetRulesModel budgetRule)
        {
            try
            {
                // Create a custom JSON object that matches Supabase expectations
                var budgetData = new
                {
                    Savings = budgetRule.Savings,
                    Needs = budgetRule.Needs,
                    Wants = budgetRule.Wants,
                    Amount = budgetRule.Amount,
                    updated_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                var json = JsonSerializer.Serialize(budgetData);
                _logger.LogInformation("JSON being sent to BudgetRules table for update: {Json}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/rest/v1/BudgetRules?UserID=eq.{budgetRule.UserID}")
                {
                    Content = content
                };
                var response = await _httpClient.SendAsync(request);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("BudgetRules update response status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("BudgetRules update response body: {Content}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to update budget rule. Status: {StatusCode}, Content: {Content}", response.StatusCode, responseContent);
                    return null;
                }

                budgetRule.updated_at = DateTime.UtcNow;
                return budgetRule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating budget rule for UserID {UserID}", budgetRule.UserID);
                return null;
            }
        }

        public async Task<bool> DeleteBudgetRuleAsync(int userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/rest/v1/BudgetRules?UserID=eq.{userId}");
                _logger.LogInformation("BudgetRules delete response status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete budget rule. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting budget rule for UserID {UserID}", userId);
                return false;
            }
        }

        public async Task<BudgetRulesModel?> UpsertBudgetRuleAsync(BudgetRulesModel budgetRule)
        {
            try
            {
                // Check if budget rule exists
                var existingRule = await GetBudgetRuleByIdAsync(budgetRule.UserID);

                if (existingRule != null)
                {
                    // Update existing rule
                    return await UpdateBudgetRuleAsync(budgetRule);
                }
                else
                {
                    // Create new rule
                    return await CreateBudgetRuleAsync(budgetRule);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting budget rule for UserID {UserID}", budgetRule.UserID);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetBudgetSummaryAsync(string userEmail)
        {
            try
            {
                var budgetRules = await GetUserBudgetRulesAsync(userEmail);
                var summary = new Dictionary<string, object>();

                if (budgetRules.Any())
                {
                    var budget = budgetRules.First();
                    summary["TotalIncome"] = budget.Amount;
                    summary["TotalSavings"] = budget.Savings;
                    summary["TotalNeeds"] = budget.Needs;
                    summary["TotalWants"] = budget.Wants;
                }
                else
                {
                    summary["TotalIncome"] = 0;
                    summary["TotalSavings"] = 0;
                    summary["TotalNeeds"] = 0;
                    summary["TotalWants"] = 0;
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting budget summary for user {UserEmail}", userEmail);
                return new Dictionary<string, object>
                {
                    ["TotalIncome"] = 0,
                    ["TotalSavings"] = 0,
                    ["TotalNeeds"] = 0,
                    ["TotalWants"] = 0
                };
            }
        }
    }
}
