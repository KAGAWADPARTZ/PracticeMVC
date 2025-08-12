using System.Text;
using System.Text.Json;
using MoneyWise.Models;

namespace MoneyWise.Services
{
    public class TransactionService
    {
        private readonly HttpClient _httpClient;
        private readonly UserRepository _userRepository;
        private readonly string _supabaseUrl;
        private readonly string _supabaseApiKey;

        public TransactionService(UserRepository userRepository, IConfiguration configuration)
        {
            _supabaseUrl = configuration["Authentication:Supabase:Url"]!;
            _supabaseApiKey = configuration["Authentication:Supabase:ApiKey"]!;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_supabaseUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseApiKey}");
            _userRepository = userRepository;
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(string userEmail)
        {
            try
            {
                var user = await GetUserByEmail(userEmail);
                if (user == null) return new List<Transaction>();

                var response = await _httpClient.GetAsync($"/rest/v1/Transactions?UserID=eq.{user.UserID.GetHashCode()}&select=*&order=created_at.desc");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            }
            catch (Exception)
            {
                return new List<Transaction>();
            }
        }

        /// <summary>
        /// Gets comprehensive transaction history with filtering, pagination, and statistics
        /// </summary>
        public async Task<TransactionHistoryResponse> GetTransactionHistoryAsync(string userEmail, TransactionHistoryFilter? filter = null)
        {
            try
            {
                var user = await GetUserByEmail(userEmail);
                if (user == null) return new TransactionHistoryResponse();

                filter ??= new TransactionHistoryFilter();

                // Build query parameters
                var queryParams = new List<string>
                {
                    $"UserID=eq.{user.UserID.GetHashCode()}",
                    "select=*"
                };

                // Add date filters
                if (filter.StartDate.HasValue)
                {
                    queryParams.Add($"created_at=gte.{filter.StartDate.Value:yyyy-MM-dd}");
                }
                if (filter.EndDate.HasValue)
                {
                    queryParams.Add($"created_at=lte.{filter.EndDate.Value:yyyy-MM-dd}");
                }

                // Add amount filters
                if (filter.MinAmount.HasValue)
                {
                    queryParams.Add($"Amount=gte.{filter.MinAmount.Value}");
                }
                if (filter.MaxAmount.HasValue)
                {
                    queryParams.Add($"Amount=lte.{filter.MaxAmount.Value}");
                }

                // Add category filter
                if (!string.IsNullOrEmpty(filter.Category))
                {
                    queryParams.Add($"Category=eq.{filter.Category}");
                }

                // Add transaction type filter
                if (!string.IsNullOrEmpty(filter.TransactionType))
                {
                    queryParams.Add($"TransactionType=eq.{filter.TransactionType}");
                }

                // Add sorting
                var sortOrder = filter.SortOrder.ToLower() == "asc" ? "asc" : "desc";
                queryParams.Add($"order={filter.SortBy}.{sortOrder}");

                // Add pagination
                var offset = (filter.Page - 1) * filter.PageSize;
                queryParams.Add($"limit={filter.PageSize}");
                queryParams.Add($"offset={offset}");

                var queryString = string.Join("&", queryParams);
                var response = await _httpClient.GetAsync($"/rest/v1/Transactions?{queryString}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();

                // Get total count for pagination
                var countQuery = queryParams.Where(p => !p.StartsWith("limit=") && !p.StartsWith("offset=") && !p.StartsWith("order="));
                var countQueryString = string.Join("&", countQuery);
                var countResponse = await _httpClient.GetAsync($"/rest/v1/Transactions?{countQueryString}&select=count");
                var countJson = await countResponse.Content.ReadAsStringAsync();
                var totalCount = ExtractCountFromResponse(countJson);

                // Calculate statistics
                var statistics = await CalculateTransactionStatisticsAsync(user.UserID.GetHashCode(), filter);

                return new TransactionHistoryResponse
                {
                    Transactions = transactions,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize),
                    Statistics = statistics
                };
            }
            catch (Exception)
            {
                return new TransactionHistoryResponse();
            }
        }

        /// <summary>
        /// Gets transaction history for a specific date range
        /// </summary>
        public async Task<List<Transaction>> GetTransactionsByDateRangeAsync(string userEmail, DateTime startDate, DateTime endDate)
        {
            try
            {
                var user = await GetUserByEmail(userEmail);
                if (user == null) return new List<Transaction>();

                var response = await _httpClient.GetAsync(
                    $"/rest/v1/Transactions?UserID=eq.{user.UserID.GetHashCode()}&created_at=gte.{startDate:yyyy-MM-dd}&created_at=lte.{endDate:yyyy-MM-dd}&select=*&order=created_at.desc");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            }
            catch (Exception)
            {
                return new List<Transaction>();
            }
        }

        /// <summary>
        /// Gets transactions by category
        /// </summary>
        public async Task<List<Transaction>> GetTransactionsByCategoryAsync(string userEmail, string category)
        {
            try
            {
                var user = await GetUserByEmail(userEmail);
                if (user == null) return new List<Transaction>();

                var response = await _httpClient.GetAsync(
                    $"/rest/v1/Transactions?UserID=eq.{user.UserID.GetHashCode()}&Category=eq.{category}&select=*&order=created_at.desc");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            }
            catch (Exception)
            {
                return new List<Transaction>();
            }
        }

        /// <summary>
        /// Searches transactions by description
        /// </summary>
        public async Task<List<Transaction>> SearchTransactionsAsync(string userEmail, string searchTerm)
        {
            try
            {
                var user = await GetUserByEmail(userEmail);
                if (user == null) return new List<Transaction>();

                var response = await _httpClient.GetAsync(
                    $"/rest/v1/Transactions?UserID=eq.{user.UserID.GetHashCode()}&Description=ilike.*{searchTerm}*&select=*&order=created_at.desc");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            }
            catch (Exception)
            {
                return new List<Transaction>();
            }
        }

        /// <summary>
        /// Gets transaction statistics for a user
        /// </summary>
        public async Task<TransactionStatistics> GetTransactionStatisticsAsync(string userEmail, TransactionHistoryFilter? filter = null)
        {
            try
            {
                var user = await GetUserByEmail(userEmail);
                if (user == null) return new TransactionStatistics();

                return await CalculateTransactionStatisticsAsync(user.UserID.GetHashCode(), filter);
            }
            catch (Exception)
            {
                return new TransactionStatistics();
            }
        }

        /// <summary>
        /// Gets recent transactions (last N transactions)
        /// </summary>
        public async Task<List<Transaction>> GetRecentTransactionsAsync(string userEmail, int count = 10)
        {
            try
            {
                var user = await GetUserByEmail(userEmail);
                if (user == null) return new List<Transaction>();

                var response = await _httpClient.GetAsync(
                    $"/rest/v1/Transactions?UserID=eq.{user.UserID.GetHashCode()}&select=*&order=created_at.desc&limit={count}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            }
            catch (Exception)
            {
                return new List<Transaction>();
            }
        }

        /// <summary>
        /// Gets monthly transaction summary
        /// </summary>
        public async Task<Dictionary<string, float>> GetMonthlyTransactionSummaryAsync(string userEmail, int year)
        {
            try
            {
                var user = await GetUserByEmail(userEmail);
                if (user == null) return new Dictionary<string, float>();

                var response = await _httpClient.GetAsync(
                    $"/rest/v1/Transactions?UserID=eq.{user.UserID.GetHashCode()}&created_at=gte.{year}-01-01&created_at=lte.{year}-12-31&select=*&order=created_at.desc");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();

                var monthlySummary = new Dictionary<string, float>();
                for (int month = 1; month <= 12; month++)
                {
                    monthlySummary[$"{year}-{month:D2}"] = 0;
                }

                foreach (var transaction in transactions)
                {
                    if (transaction.created_at.HasValue)
                    {
                        var monthKey = $"{transaction.created_at.Value:yyyy-MM}";
                        monthlySummary[monthKey] += transaction.Amount;
                    }
                }

                return monthlySummary;
            }
            catch (Exception)
            {
                return new Dictionary<string, float>();
            }
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int transactionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/rest/v1/Transactions?TransactionID=eq.{transactionId}&select=*");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Transaction>>(json)?.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> CreateTransactionAsync(Transaction transaction)
        {
            var json = JsonSerializer.Serialize(transaction);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/rest/v1/Transactions", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTransactionAsync(int id, Transaction transaction)
        {
            var json = JsonSerializer.Serialize(transaction);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/rest/v1/Transactions?TransactionID=eq.{id}")
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteTransactionAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/rest/v1/Transactions?TransactionID=eq.{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<(bool success, string message)> SaveUserTransactionAsync(string? userEmail, TransactionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(userEmail))
                {
                    return (false, "User not authenticated");
                }

                var user = await GetUserByEmail(userEmail);
                if (user == null)
                {
                    return (false, "User not found");
                }

                // Determine transaction type based on action
                var transactionType = request.Action.ToLower() switch
                {
                    "add" => "income",
                    "withdraw" => "expense",
                    _ => "transfer"
                };

                // Create transaction
                var transaction = new Transaction
                {
                    TransactionID = 0, // Auto-generated by database
                    UserID = user.UserID.GetHashCode(),
                    Amount = request.Amount,
                    Description = request.Description,
                    Category = request.Category ?? "General",
                    TransactionType = transactionType,
                    created_at = DateTime.UtcNow
                };

                var success = await CreateTransactionAsync(transaction);
                
                if (success)
                {
                    var actionText = request.Action == "add" ? "added to" : "withdrawn from";
                    return (true, $"₱{Math.Abs(request.Amount):F2} successfully {actionText} your savings.");
                }
                else
                {
                    return (false, "Failed to save transaction");
                }
            }
            catch (Exception)
            {
                return (false, "An error occurred while saving the transaction");
            }
        }

        // Private helper methods
        private async Task<Users?> GetUserByEmail(string userEmail)
        {
            var users = await _userRepository.GetAllUsers();
            return users.FirstOrDefault(u => u.Email == userEmail);
        }

        private async Task<TransactionStatistics> CalculateTransactionStatisticsAsync(int userId, TransactionHistoryFilter? filter = null)
        {
            try
            {
                var allTransactions = await GetUserTransactionsAsync(userId.ToString());
                
                if (filter != null)
                {
                    allTransactions = ApplyFilterToTransactions(allTransactions, filter);
                }

                var statistics = new TransactionStatistics
                {
                    TotalTransactions = allTransactions.Count,
                    TotalIncome = allTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
                    TotalExpenses = allTransactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount)),
                    LargestTransaction = allTransactions.Any() ? allTransactions.Max(t => Math.Abs(t.Amount)) : 0,
                    SmallestTransaction = allTransactions.Any() ? allTransactions.Min(t => Math.Abs(t.Amount)) : 0
                };

                statistics.NetAmount = statistics.TotalIncome - statistics.TotalExpenses;
                statistics.AverageTransactionAmount = allTransactions.Any() ? allTransactions.Average(t => Math.Abs(t.Amount)) : 0;

                // Calculate category statistics
                var categoryGroups = allTransactions.GroupBy(t => t.Category ?? "Uncategorized");
                foreach (var group in categoryGroups)
                {
                    statistics.CategoryTotals[group.Key] = group.Sum(t => t.Amount);
                    statistics.CategoryCounts[group.Key] = group.Count();
                }

                return statistics;
            }
            catch (Exception)
            {
                return new TransactionStatistics();
            }
        }

        private List<Transaction> ApplyFilterToTransactions(List<Transaction> transactions, TransactionHistoryFilter filter)
        {
            var filtered = transactions.AsEnumerable();

            if (filter.StartDate.HasValue)
            {
                filtered = filtered.Where(t => t.created_at >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                filtered = filtered.Where(t => t.created_at <= filter.EndDate.Value);
            }

            if (filter.MinAmount.HasValue)
            {
                filtered = filtered.Where(t => Math.Abs(t.Amount) >= filter.MinAmount.Value);
            }

            if (filter.MaxAmount.HasValue)
            {
                filtered = filtered.Where(t => Math.Abs(t.Amount) <= filter.MaxAmount.Value);
            }

            if (!string.IsNullOrEmpty(filter.Category))
            {
                filtered = filtered.Where(t => t.Category == filter.Category);
            }

            if (!string.IsNullOrEmpty(filter.TransactionType))
            {
                filtered = filtered.Where(t => t.TransactionType == filter.TransactionType);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                filtered = filtered.Where(t => 
                    t.Description?.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) == true);
            }

            return filtered.ToList();
        }

        private int ExtractCountFromResponse(string json)
        {
            try
            {
                // Supabase count response format: [{"count": 123}]
                var countArray = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
                if (countArray?.FirstOrDefault()?.TryGetValue("count", out var countValue) == true)
                {
                    return Convert.ToInt32(countValue);
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}