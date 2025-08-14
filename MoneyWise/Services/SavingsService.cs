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

                // Get current savings balance from Savings table
                var currentSavings = await _supabaseService.GetSavingsByUserIdAsync(user.UserID);
                
                if (currentSavings == null)
                {
                    // If no savings record exists, create one with zero balance
                    var newSavings = new Savings
                    {
                        TransactionID = 0,
                        UserID = user.UserID,
                        Amount = 0,
                        created_at = DateTime.UtcNow
                    };
                    
                    await _supabaseService.CreateSavingsAsync(newSavings);
                    return newSavings;
                }

                return currentSavings;
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

                // Get current savings balance
                var currentSavings = await GetUserSavingsAsync(userEmail);
                if (currentSavings == null)
                {
                    return (false, "Failed to retrieve current savings balance");
                }

                // Calculate new balance
                var newBalance = currentSavings.Amount + (request.Action == "deposit" ? request.SavingsAmount : -request.SavingsAmount);

                // Check if withdrawal would result in negative balance
                if (request.Action == "withdraw" && newBalance < 0)
                {
                    return (false, "Insufficient funds for withdrawal");
                }

                // Update the current balance in Savings table
                var updatedSavings = new Savings
                {
                    TransactionID = currentSavings.TransactionID,
                    UserID = user.UserID,
                    Amount = newBalance,
                    created_at = currentSavings.created_at,
                    updated_at = DateTime.UtcNow
                };

                var updateSuccess = await _supabaseService.UpdateSavingsAsync(currentSavings.TransactionID, updatedSavings);

                if (updateSuccess)
                {
                    // Create transaction history record in Histories table
                    var historyRequest = new HistoryRequest
                    {
                        Amount = (float)request.SavingsAmount,
                        Type = request.Action,
                        Description = $"{request.Action} transaction"
                    };

                    var historySuccess = await _historyService.CreateHistoryAsync(userEmail, historyRequest);

                    if (historySuccess)
                    {
                        var actionText = request.Action == "deposit" ? "deposited" : "withdrawn";
                        return (true, $"₱{request.SavingsAmount:F2} successfully {actionText}. New balance: ₱{newBalance:F2}");
                    }
                    else
                    {
                        // Transaction was updated but history failed - still return success for the transaction
                        var actionText = request.Action == "deposit" ? "deposited" : "withdrawn";
                        return (true, $"₱{request.SavingsAmount:F2} successfully {actionText}. New balance: ₱{newBalance:F2} (History recording failed)");
                    }
                }
                else
                {
                    return (false, "Failed to update savings balance.");
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