using MoneyWise.Models;
using System.Text.Json;
using System.Text;

namespace MoneyWise.Services
{
    public class SavingsService
    {
        private readonly SupabaseService _supabaseService;
        private readonly UserRepository _userRepository;
        private readonly HistoryService _historyService;

        public SavingsService(SupabaseService supabaseService, UserRepository userRepository, HistoryService historyService)
        {
            _supabaseService = supabaseService;
            _userRepository = userRepository;
            _historyService = historyService;
        }

        public async Task<Savings?> GetUserSavingsAsync(string userEmail)
        {
            try
            {
                // Get user from database
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return null;
                }

                // Get all transactions for the user
                var allTransactions = await _supabaseService.GetTransactionsByUserIdAsync(user.UserID);
                
                if (allTransactions == null || !allTransactions.Any())
                {
                    return null;
                }

                // Calculate current balance from all transactions
                var currentBalance = allTransactions.Sum(t => t.Amount);
                
                // Get the most recent transaction for metadata
                var latestTransaction = allTransactions.OrderByDescending(t => t.created_at).First();
                
                // Return a summary object with the calculated balance
                return new Savings
                {
                    TransactionID = latestTransaction.TransactionID,
                    UserID = user.UserID,
                    Amount = currentBalance,
                    created_at = latestTransaction.created_at,
                    updated_at = latestTransaction.updated_at
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> CreateSavingsAsync(Savings savings)
        {
            try
            {
                return await _supabaseService.CreateSavingsAsync(savings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateSavingsAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateSavingsAsync(int savingsId, Savings savings)
        {
            try
            {
                return await _supabaseService.UpdateSavingsAsync(savingsId, savings);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<(bool success, string message)> SaveUserSavingsAsync(string userEmail, SavingsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(userEmail))
                    return (false, "User not authenticated");

                // Get user from database
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);

                if (user == null)
                    return (false, $"User not found. Email: {userEmail}");

                // Always create a new transaction record for each deposit/withdrawal
                var newTransaction = new Savings
                {
                    TransactionID = 0, // Let database auto-generate the primary key
                    UserID = user.UserID,
                    Amount = request.Action == "deposit" ? request.SavingsAmount : -request.SavingsAmount,
                    created_at = DateTime.UtcNow
                };

                var createSuccess = await _supabaseService.CreateSavingsAsync(newTransaction);

                if (createSuccess)
                {
                    // Create history record for the transaction
                    var historyType = request.Action == "deposit" ? "deposit" : "withdrawal";
                    var historyAmount = request.SavingsAmount;
                    
                    var historySuccess = await _historyService.CreateHistoryRecordAsync(userEmail, historyType, historyAmount);
                    
                    if (!historySuccess)
                    {
                        Console.WriteLine("Warning: Failed to create history record, but transaction was successful");
                    }

                    var actionText = request.Action == "deposit" ? "deposited" : "withdrawn";
                    return (true, $"₱{request.SavingsAmount:F2} successfully {actionText}.");
                }
                else
                {
                    return (false, "Failed to create transaction record.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SaveUserSavingsAsync: {ex.Message}");
                return (false, "An error occurred while processing the transaction");
            }
        }

    }
} 