using MoneyWise.Models;
using System.Text.Json;

namespace MoneyWise.Services
{
    public class SavingsCalculatorService
    {
        private readonly SupabaseService _supabaseService;
        private readonly UserRepository _userRepository;

        public SavingsCalculatorService(SupabaseService supabaseService, UserRepository userRepository)
        {
            _supabaseService = supabaseService;
            _userRepository = userRepository;
        }

        public async Task<SavingsSummary> CalculateUserSavingsSummaryAsync(string userEmail)
        {
            try
            {
                // Get user from database
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);

                if (user == null)
                {
                    return new SavingsSummary
                    {
                        TotalSavings = 0,
                        TotalWithdrawals = 0,
                        CurrentBalance = 0,
                        UserId = 0  // Changed from Guid.Empty to 0
                    };
                }

                // Get current savings balance from Savings table (total amount only)
                var currentSavings = await _supabaseService.GetSavingsByUserIdAsync(user.UserID);
                
                // Get transaction history from Histories table
                var histories = await _supabaseService.GetHistoriesByUserIdAsync(user.UserID);

                decimal totalSavings = 0;
                decimal totalWithdrawals = 0;

                foreach (var history in histories)
                {
                    if (history.Type == "deposit")
                    {
                        // Deposits are positive amounts
                        totalSavings += (decimal)history.Amount;
                    }
                    else if (history.Type == "withdrawal")
                    {
                        // Withdrawals are positive amounts in history
                        totalWithdrawals += (decimal)history.Amount;
                    }
                }

                decimal currentBalance = currentSavings?.Amount ?? 0;

                return new SavingsSummary
                {
                    TotalSavings = totalSavings,
                    TotalWithdrawals = totalWithdrawals,
                    CurrentBalance = currentBalance,
                    UserId = user.UserID,
                    TransactionCount = histories.Count
                };
            }
            catch (Exception)
            {
                return new SavingsSummary
                {
                    TotalSavings = 0,
                    TotalWithdrawals = 0,
                    CurrentBalance = 0,
                    UserId = 0,
                    TransactionCount = 0
                };
            }
        }

        public async Task<List<TransactionSummary>> GetUserTransactionHistoryAsync(string userEmail)
        {
            try
            {
                // Get user from database
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);

                if (user == null)
                {
                    return new List<TransactionSummary>();
                }

                // Get transaction history from Histories table
                var histories = await _supabaseService.GetHistoriesByUserIdAsync(user.UserID);

                var transactionSummaries = new List<TransactionSummary>();

                foreach (var history in histories)
                {
                    var amount = (decimal)history.Amount;
                    var formattedAmount = history.Type == "withdrawal" ? $"-₱{amount:F2}" : $"₱{amount:F2}";

                    transactionSummaries.Add(new TransactionSummary
                    {
                        Amount = amount,
                        Type = history.Type,
                        Date = history.created_at ?? DateTime.UtcNow,
                        FormattedAmount = formattedAmount
                    });
                }

                return transactionSummaries.OrderByDescending(t => t.Date).ToList();
            }
            catch (Exception)
            {
                return new List<TransactionSummary>();
            }
        }

        public async Task<Dictionary<string, decimal>> GetMonthlyEarningsAsync(string userEmail)
        {
            var users = await _userRepository.GetAllUsers();
            var user = users.FirstOrDefault(u => u.Email == userEmail);
            if (user == null) return new Dictionary<string, decimal>();

            var histories = await _supabaseService.GetHistoriesByUserIdAsync(user.UserID);
            var currentYear = DateTime.UtcNow.Year;

            var monthlyTotals = histories
                .Where(h => h.Type == "deposit" && h.created_at?.Year == currentYear)
                .GroupBy(h => h.created_at!.Value.Month)
                .ToDictionary(
                    g => new DateTime(currentYear, g.Key, 1).ToString("MMMM"),
                    g => (decimal)g.Sum(h => h.Amount)
                );

            // Ensure all 12 months are represented (even if 0)
            var result = new Dictionary<string, decimal>();
            for (int month = 1; month <= 12; month++)
            {
                var monthName = new DateTime(currentYear, month, 1).ToString("MMMM");
                result[monthName] = monthlyTotals.ContainsKey(monthName) ? monthlyTotals[monthName] : 0;
            }

            return result;
        }


    }

    public class SavingsSummary
    {
        public decimal TotalSavings { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal CurrentBalance { get; set; }
        public int UserId { get; set; }
        public int TransactionCount { get; set; }
    }

    public class TransactionSummary
    {
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string FormattedAmount { get; set; } = string.Empty;
    }
}