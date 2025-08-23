using System.Text;
using System.Text.Json;
using MoneyWise.Models;
using Microsoft.Extensions.Logging;

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

            _logger.LogInformation("BudgetRulesService initialized with Supabase URL: {Url}", _supabaseUrl);
        }

        public async Task<List<BudgetRules>> GetAllBudgetRulesAsync()
        {
            try
            {
                _logger.LogInformation("Calling Supabase API to get all budget rules...");
                var response = await _httpClient.GetAsync("/rest/v1/BudgetRules?select=*");
                
                _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase error: {Error}", errorContent);
                    return new List<BudgetRules>();
                }
                
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response JSON: {Json}", json);
                
                var budgetRules = JsonSerializer.Deserialize<List<BudgetRules>>(json);
                _logger.LogInformation("Deserialized budget rules count: {Count}", budgetRules?.Count ?? 0);
                
                return budgetRules ?? new List<BudgetRules>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetAllBudgetRulesAsync");
                return new List<BudgetRules>();
            }
        }

        public async Task<BudgetRules?> GetBudgetRuleByIdAsync(int budgetRuleId)
        {
            try
            {
                _logger.LogInformation("Looking for budget rule with ID: {Id}", budgetRuleId);
                var url = $"/rest/v1/BudgetRules?BudgetRulesID=eq.{budgetRuleId}&select=*";
                _logger.LogInformation("Supabase URL: {Url}", url);
                
                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase error: {Error}", errorContent);
                    return null;
                }
                
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response JSON: {Json}", json);
                
                var budgetRules = JsonSerializer.Deserialize<List<BudgetRules>>(json);
                var budgetRule = budgetRules?.FirstOrDefault();
                _logger.LogInformation("Found budget rule: {Id}", budgetRule?.BudgetRulesID);
                
                return budgetRule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetBudgetRuleByIdAsync for ID: {Id}", budgetRuleId);
                return null;
            }
        }

        public async Task<List<BudgetRules>> GetBudgetRulesByUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Looking for budget rules for user ID: {UserId}", userId);
                var url = $"/rest/v1/BudgetRules?UserID=eq.{userId}&select=*";
                _logger.LogInformation("Supabase URL: {Url}", url);
                
                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase error: {Error}", errorContent);
                    return new List<BudgetRules>();
                }
                
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response JSON: {Json}", json);
                
                var budgetRules = JsonSerializer.Deserialize<List<BudgetRules>>(json);
                _logger.LogInformation("Found budget rules count: {Count}", budgetRules?.Count ?? 0);
                
                return budgetRules ?? new List<BudgetRules>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetBudgetRulesByUserAsync for UserID: {UserId}", userId);
                return new List<BudgetRules>();
            }
        }

        public async Task<BudgetRules?> CreateBudgetRuleAsync(BudgetRules budgetRule)
        {
            try
            {
                _logger.LogInformation("Creating new budget rule for user: {UserId}", budgetRule.UserID);
                
                // Set the updated timestamp
                budgetRule.updated_at = DateTime.UtcNow;
                
                var json = JsonSerializer.Serialize(budgetRule);
                _logger.LogInformation("JSON being sent: {Json}", json);
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/rest/v1/BudgetRules", content);
                
                _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase error: {Error}", errorContent);
                    return null;
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response JSON: {Json}", responseJson);
                
                var createdBudgetRule = JsonSerializer.Deserialize<BudgetRules>(responseJson);
                _logger.LogInformation("Created budget rule with ID: {Id}", createdBudgetRule?.BudgetRulesID);
                
                return createdBudgetRule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in CreateBudgetRuleAsync");
                return null;
            }
        }

        public async Task<BudgetRules?> UpdateBudgetRuleAsync(BudgetRules budgetRule)
        {
            try
            {
                _logger.LogInformation("Updating budget rule with ID: {Id}", budgetRule.BudgetRulesID);
                
                // Set the updated timestamp
                budgetRule.updated_at = DateTime.UtcNow;
                
                var json = JsonSerializer.Serialize(budgetRule);
                _logger.LogInformation("JSON being sent: {Json}", json);
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/rest/v1/BudgetRules?BudgetRulesID=eq.{budgetRule.BudgetRulesID}")
                {
                    Content = content
                };
                var response = await _httpClient.SendAsync(request);
                
                _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase error: {Error}", errorContent);
                    return null;
                }
                
                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response JSON: {Json}", responseJson);
                
                var updatedBudgetRule = JsonSerializer.Deserialize<BudgetRules>(responseJson);
                _logger.LogInformation("Updated budget rule with ID: {Id}", updatedBudgetRule?.BudgetRulesID);
                
                return updatedBudgetRule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateBudgetRuleAsync for ID: {Id}", budgetRule.BudgetRulesID);
                return null;
            }
        }

        public async Task<bool> DeleteBudgetRuleAsync(int budgetRuleId)
        {
            try
            {
                _logger.LogInformation("Deleting budget rule with ID: {Id}", budgetRuleId);
                
                var response = await _httpClient.DeleteAsync($"/rest/v1/BudgetRules?BudgetRulesID=eq.{budgetRuleId}");
                
                _logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Supabase error: {Error}", errorContent);
                    return false;
                }
                
                _logger.LogInformation("Successfully deleted budget rule with ID: {Id}", budgetRuleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in DeleteBudgetRuleAsync for ID: {Id}", budgetRuleId);
                return false;
            }
        }

        public async Task<BudgetRules?> UpsertBudgetRuleAsync(BudgetRules budgetRule)
        {
            try
            {
                _logger.LogInformation("Upserting budget rule with ID: {Id}", budgetRule.BudgetRulesID);
                
                // Check if budget rule exists
                var existingRule = await GetBudgetRuleByIdAsync(budgetRule.BudgetRulesID);
                
                if (existingRule != null)
                {
                    // Update existing rule
                    _logger.LogInformation("Budget rule exists, updating...");
                    return await UpdateBudgetRuleAsync(budgetRule);
                }
                else
                {
                    // Create new rule
                    _logger.LogInformation("Budget rule doesn't exist, creating new...");
                    return await CreateBudgetRuleAsync(budgetRule);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpsertBudgetRuleAsync for ID: {Id}", budgetRule.BudgetRulesID);
                return null;
            }
        }

        public async Task<Dictionary<string, object>> GetBudgetSummaryAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Getting budget summary for user ID: {UserId}", userId);
                
                var budgetRules = await GetBudgetRulesByUserAsync(userId);
                var summary = new Dictionary<string, object>();

                if (budgetRules.Any())
                {
                    var latestRule = budgetRules.OrderByDescending(b => b.updated_at).First();
                    summary["TotalIncome"] = latestRule.TotalAmount;
                    summary["TotalSavings"] = latestRule.SavingsAmount;
                    summary["TotalNeeds"] = latestRule.NeedsAmount;
                    summary["TotalWants"] = latestRule.WantsAmount;
                    summary["SavingsPercentage"] = latestRule.TotalAmount > 0 ? (latestRule.SavingsAmount * 100.0 / latestRule.TotalAmount) : 0;
                    summary["NeedsPercentage"] = latestRule.TotalAmount > 0 ? (latestRule.NeedsAmount * 100.0 / latestRule.TotalAmount) : 0;
                    summary["WantsPercentage"] = latestRule.TotalAmount > 0 ? (latestRule.WantsAmount * 100.0 / latestRule.TotalAmount) : 0;
                }
                else
                {
                    summary["TotalIncome"] = 0;
                    summary["TotalSavings"] = 0;
                    summary["TotalNeeds"] = 0;
                    summary["TotalWants"] = 0;
                    summary["SavingsPercentage"] = 0;
                    summary["NeedsPercentage"] = 0;
                    summary["WantsPercentage"] = 0;
                }

                _logger.LogInformation("Budget summary calculated for user ID: {UserId}", userId);
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetBudgetSummaryAsync for UserID: {UserId}", userId);
                return new Dictionary<string, object>
                {
                    ["TotalIncome"] = 0,
                    ["TotalSavings"] = 0,
                    ["TotalNeeds"] = 0,
                    ["TotalWants"] = 0,
                    ["SavingsPercentage"] = 0,
                    ["NeedsPercentage"] = 0,
                    ["WantsPercentage"] = 0
                };
            }
        }

        public async Task<bool> ValidateBudgetPercentagesAsync(int savings, int needs, int wants)
        {
            try
            {
                var total = savings + needs + wants;
                var isValid = Math.Abs(total - 100) <= 1; // Allow 1% tolerance for rounding
                
                _logger.LogInformation("Budget percentage validation: Savings={Savings}%, Needs={Needs}%, Wants={Wants}%, Total={Total}%, Valid={Valid}", 
                    savings, needs, wants, total, isValid);
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ValidateBudgetPercentagesAsync");
                return false;
            }
        }
    }
}
