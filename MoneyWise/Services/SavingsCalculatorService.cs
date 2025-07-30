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

                // Get all transactions for the user
                var transactions = await GetUserTransactionsAsync(user.UserID);

                decimal totalSavings = 0;
                decimal totalWithdrawals = 0;

                foreach (var transaction in transactions)
                {
                    if (transaction.Amount > 0)
                    {
                        // Positive amounts are savings/deposits
                        totalSavings += transaction.Amount;
                    }
                    else
                    {
                        // Negative amounts are withdrawals
                        totalWithdrawals += Math.Abs(transaction.Amount);
                    }
                }

                decimal currentBalance = totalSavings - totalWithdrawals;

                return new SavingsSummary
                {
                    TotalSavings = totalSavings,
                    TotalWithdrawals = totalWithdrawals,
                    CurrentBalance = currentBalance,
                    UserId = user.UserID,
                    TransactionCount = transactions.Count
                };
            }
            catch (Exception)
            {
                return new SavingsSummary
                {
                    TotalSavings = 0,
                    TotalWithdrawals = 0,
                    CurrentBalance = 0,
                    UserId = 0  // Changed from Guid.Empty to 0
                };
            }
        }

        private async Task<List<Savings>> GetUserTransactionsAsync(int userId)
        {
            try
            {
                var response = await _supabaseService.GetTransactionsByUserIdAsync(userId);
                return response ?? new List<Savings>();  // Fixed the null coalescing operator
            }
            catch (Exception)
            {
                return new List<Savings>();
            }
        }

        public async Task<List<TransactionSummary>> GetRecentTransactionsAsync(string userEmail, int limit = 10)
        {
            try
            {
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);

                if (user == null)
                {
                    return new List<TransactionSummary>();
                }

                var transactions = await GetUserTransactionsAsync(user.UserID);

                return transactions
                    .Take(limit)
                    .Select(t => new TransactionSummary
                    {
                        Amount = t.Amount,
                        Type = t.Amount > 0 ? "Savings" : "Withdrawal",
                        Date = t.created_at ?? DateTime.UtcNow,
                        FormattedAmount = t.Amount > 0 ? $"+₱{t.Amount:F2}" : $"-₱{Math.Abs(t.Amount):F2}"
                    })
                    .ToList();
            }
            catch (Exception)
            {
                return new List<TransactionSummary>();
            }
        }
        public async Task<decimal> CalculateAnnualEarningsAsync(string userEmail)
        {
            var users = await _userRepository.GetAllUsers();
            var user = users.FirstOrDefault(u => u.Email == userEmail);

            if (user == null)
                return 0;

            var transactions = await GetUserTransactionsAsync(user.UserID);
            var currentYear = DateTime.UtcNow.Year;

            // Sum all deposits (positive amounts) made this year
            var totalAnnualEarnings = transactions
                .Where(t => t.Amount > 0 && t.created_at?.Year == currentYear)
                .Sum(t => t.Amount);

            return totalAnnualEarnings;
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