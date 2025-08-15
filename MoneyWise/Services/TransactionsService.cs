using System.Text;
using System.Text.Json;
using MoneyWise.Models;

namespace MoneyWise.Services
{
    public class TransactionService
    {
        private readonly HttpClient _httpClient;
        private readonly UserRepository _userRepository;
        private readonly SupabaseService _supabaseService;
        private readonly string _supabaseUrl;
        private readonly string _supabaseApiKey;

        public TransactionService(UserRepository userRepository, SupabaseService supabaseService, IConfiguration configuration)
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
            _supabaseService = supabaseService;
        }

        public async Task<List<HistoryModel>> GetUserTransactionsAsync(string userEmail)
        {
            try
            {
                // Get user from database first
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return new List<HistoryModel>();
                }

                var response = await _httpClient.GetAsync($"/rest/v1/Histories?UserID=eq.{user.UserID}&select=*&order=created_at.desc");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<HistoryModel>>(json) ?? new List<HistoryModel>();
            }
            catch (Exception)
            {
                return new List<HistoryModel>();
            }
        }

        public async Task<HistoryModel?> GetTransactionByIdAsync(int transactionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/rest/v1/Histories?HistoryID=eq.{transactionId}&select=*");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<HistoryModel>>(json)?.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> CreateTransactionAsync(HistoryModel transaction)
        {
            var json = JsonSerializer.Serialize(transaction);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/rest/v1/Histories", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTransactionAsync(int id, HistoryModel transaction)
        {
            var json = JsonSerializer.Serialize(transaction);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/rest/v1/Histories?HistoryID=eq.{id}")
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
                var response = await _httpClient.DeleteAsync($"/rest/v1/Histories?HistoryID=eq.{id}");
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

                // Get user from database
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return (false, "User not found");
                }

                // Get current savings balance
                var currentSavings = await _supabaseService.GetSavingsByUserIdAsync(user.UserID);
                if (currentSavings == null)
                {
                    // Create new savings record if none exists
                    currentSavings = new Savings
                    {
                        SavingsID = 0,
                        UserID = user.UserID,
                        Amount = 0,
                        created_at = DateTime.UtcNow
                    };
                    await _supabaseService.CreateSavingsAsync(currentSavings);
                }

                // Calculate new balance based on transaction type
                decimal newBalance;
                string actionText;
                
                if (request.Action.ToLower() == "add" || request.Action.ToLower() == "deposit")
                {
                    newBalance = currentSavings.Amount + (decimal)request.Amount;
                    actionText = "added to";
                }
                else if (request.Action.ToLower() == "withdraw" || request.Action.ToLower() == "withdrawal")
                {
                    // Check if withdrawal would result in negative balance
                    if (currentSavings.Amount < (decimal)request.Amount)
                    {
                        return (false, "Insufficient funds for withdrawal");
                    }
                    newBalance = currentSavings.Amount - (decimal)request.Amount;
                    actionText = "withdrawn from";
                }
                else
                {
                    return (false, "Invalid transaction type. Use 'add', 'deposit', 'withdraw', or 'withdrawal'");
                }

                // Update the Savings table with new balance
                var updatedSavings = new Savings
                {
                    SavingsID = currentSavings.SavingsID,
                    UserID = user.UserID,
                    Amount = newBalance,
                    created_at = currentSavings.created_at,
                    updated_at = DateTime.UtcNow
                };

                var savingsUpdateSuccess = await _supabaseService.UpdateSavingsAsync(currentSavings.SavingsID, updatedSavings);
                
                if (!savingsUpdateSuccess)
                {
                    return (false, "Failed to update savings balance");
                }

                // Create transaction history in Histories table
                var transaction = new HistoryModel
                {
                    HistoryID = 0, // Auto-generated by database
                    UserID = user.UserID,
                    Type = request.Action.ToLower() == "add" ? "deposit" : 
                           request.Action.ToLower() == "withdraw" ? "withdrawal" : request.Action,
                    Amount = (float)request.Amount,
                    created_at = DateTime.UtcNow
                };

                var historySuccess = await CreateTransactionAsync(transaction);
                
                if (historySuccess)
                {
                    return (true, $"₱{Math.Abs(request.Amount):F2} successfully {actionText} your savings. New balance: ₱{newBalance:F2}");
                }
                else
                {
                    // Transaction was updated but history failed - still return success for the transaction
                    return (true, $"₱{Math.Abs(request.Amount):F2} successfully {actionText} your savings. New balance: ₱{newBalance:F2} (History recording failed)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SaveUserTransactionAsync: {ex.Message}");
                return (false, "An error occurred while processing the transaction");
            }
        }

        // New methods for enhanced transaction history functionality

        public async Task<List<HistoryModel>> GetRecentTransactionsAsync(string userEmail, int count = 10)
        {
            try
            {
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return new List<HistoryModel>();
                }

                var response = await _httpClient.GetAsync($"/rest/v1/Histories?UserID=eq.{user.UserID}&select=*&order=created_at.desc&limit={count}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<HistoryModel>>(json) ?? new List<HistoryModel>();
            }
            catch (Exception)
            {
                return new List<HistoryModel>();
            }
        }

        public async Task<List<HistoryModel>> GetTransactionsByDateRangeAsync(string userEmail, DateTime startDate, DateTime endDate)
        {
            try
            {
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return new List<HistoryModel>();
                }

                var startDateStr = startDate.ToString("yyyy-MM-dd");
                var endDateStr = endDate.ToString("yyyy-MM-dd");
                
                var response = await _httpClient.GetAsync($"/rest/v1/Histories?UserID=eq.{user.UserID}&created_at=gte.{startDateStr}&created_at=lte.{endDateStr}&select=*&order=created_at.desc");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<HistoryModel>>(json) ?? new List<HistoryModel>();
            }
            catch (Exception)
            {
                return new List<HistoryModel>();
            }
        }

        public async Task<TransactionStatistics> GetTransactionStatisticsAsync(string userEmail)
        {
            try
            {
                var histories = await GetUserTransactionsAsync(userEmail);
                
                var deposits = histories.Where(h => h.Type == "deposit").ToList();
                var withdrawals = histories.Where(h => h.Type == "withdrawal").ToList();

                return new TransactionStatistics
                {
                    TotalTransactions = histories.Count,
                    TotalDeposits = deposits.Sum(h => h.Amount),
                    TotalWithdrawals = withdrawals.Sum(h => h.Amount),
                    AverageDeposit = deposits.Any() ? deposits.Average(h => h.Amount) : 0,
                    AverageWithdrawal = withdrawals.Any() ? withdrawals.Average(h => h.Amount) : 0
                };
            }
            catch (Exception)
            {
                return new TransactionStatistics();
            }
        }

        public async Task<List<MonthlyTransactionSummary>> GetMonthlyTransactionSummaryAsync(string userEmail, int year)
        {
            try
            {
                var transactions = await GetUserTransactionsAsync(userEmail);
                var yearTransactions = transactions.Where(t => t.created_at?.Year == year).ToList();

                var monthlySummaries = new List<MonthlyTransactionSummary>();
                
                for (int month = 1; month <= 12; month++)
                {
                    var monthTransactions = yearTransactions.Where(t => t.created_at?.Month == month).ToList();
                    var deposits = monthTransactions.Where(t => t.Type == "deposit").Sum(t => t.Amount);
                    var withdrawals = monthTransactions.Where(t => t.Type == "withdrawal").Sum(t => t.Amount);
                    
                    monthlySummaries.Add(new MonthlyTransactionSummary
                    {
                        Month = month,
                        MonthName = new DateTime(year, month, 1).ToString("MMMM"),
                        TotalDeposits = deposits,
                        TotalWithdrawals = withdrawals,
                        NetAmount = deposits - withdrawals,
                        TransactionCount = monthTransactions.Count
                    });
                }

                return monthlySummaries;
            }
            catch (Exception)
            {
                return new List<MonthlyTransactionSummary>();
            }
        }

        public async Task<TransactionHistoryResponse> GetTransactionHistoryAsync(string userEmail, TransactionHistoryFilter? filter = null)
        {
            try
            {
                var transactions = await GetUserTransactionsAsync(userEmail);
                
                if (filter != null)
                {
                    // Apply date range filter
                    if (filter.StartDate.HasValue)
                    {
                        transactions = transactions.Where(t => t.created_at >= filter.StartDate.Value).ToList();
                    }
                    
                    if (filter.EndDate.HasValue)
                    {
                        transactions = transactions.Where(t => t.created_at <= filter.EndDate.Value).ToList();
                    }

                    // Apply amount range filter
                    if (filter.MinAmount.HasValue)
                    {
                        transactions = transactions.Where(t => t.Amount >= filter.MinAmount.Value).ToList();
                    }
                    
                    if (filter.MaxAmount.HasValue)
                    {
                        transactions = transactions.Where(t => t.Amount <= filter.MaxAmount.Value).ToList();
                    }

                    // Apply transaction type filter
                    if (!string.IsNullOrEmpty(filter.TransactionType))
                    {
                        if (filter.TransactionType.ToLower() == "deposit")
                        {
                            transactions = transactions.Where(t => t.Type == "deposit").ToList();
                        }
                        else if (filter.TransactionType.ToLower() == "withdrawal")
                        {
                            transactions = transactions.Where(t => t.Type == "withdrawal").ToList();
                        }
                    }
                }

                var statistics = await GetTransactionStatisticsAsync(userEmail);
                
                return new TransactionHistoryResponse
                {
                    Transactions = transactions,
                    Statistics = statistics,
                    TotalCount = transactions.Count
                };
            }
            catch (Exception)
            {
                return new TransactionHistoryResponse
                {
                    Transactions = new List<HistoryModel>(),
                    Statistics = new TransactionStatistics(),
                    TotalCount = 0
                };
            }
        }
    }

    // Supporting classes for enhanced functionality
    public class TransactionStatistics
    {
        public int TotalTransactions { get; set; }
        public float TotalDeposits { get; set; }
        public float TotalWithdrawals { get; set; }
        public float NetBalance { get; set; }
        public float AverageDeposit { get; set; }
        public float AverageWithdrawal { get; set; }
        public float LargestDeposit { get; set; }
        public float LargestWithdrawal { get; set; }
    }

    public class MonthlyTransactionSummary
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public float TotalDeposits { get; set; }
        public float TotalWithdrawals { get; set; }
        public float NetAmount { get; set; }
        public int TransactionCount { get; set; }
    }

    public class TransactionHistoryFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public float? MinAmount { get; set; }
        public float? MaxAmount { get; set; }
        public string? TransactionType { get; set; } // "deposit" or "withdrawal"
    }

    public class TransactionHistoryResponse
    {
        public List<HistoryModel> Transactions { get; set; } = new List<HistoryModel>();
        public TransactionStatistics Statistics { get; set; } = new TransactionStatistics();
        public int TotalCount { get; set; }
    }
}