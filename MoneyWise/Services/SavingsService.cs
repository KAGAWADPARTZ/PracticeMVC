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
                        SavingsID = 0,
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

                // Calculate new balance based on transaction type
                decimal newBalance;
                string actionText;
                
                if (request.Action.ToLower() == "deposit")
                {
                    newBalance = currentSavings.Amount + request.SavingsAmount;
                    actionText = "deposited";
                }
                else if (request.Action.ToLower() == "withdraw")
                {
                    // Check if withdrawal would result in negative balance
                    if (currentSavings.Amount < request.SavingsAmount)
                    {
                        return (false, "Insufficient funds for withdrawal");
                    }
                    newBalance = currentSavings.Amount - request.SavingsAmount;
                    actionText = "withdrawn";
                }
                else
                {
                    return (false, "Invalid transaction type. Use 'deposit' or 'withdraw'");
                }

                // Update the current balance in Savings table
                var updatedSavings = new Savings
                {
                    SavingsID = currentSavings.SavingsID,
                    UserID = user.UserID,
                    Amount = newBalance,
                    created_at = currentSavings.created_at,
                    updated_at = DateTime.UtcNow
                };

                var updateSuccess = await _supabaseService.UpdateSavingsAsync(currentSavings.SavingsID, updatedSavings);

                if (updateSuccess)
                {
                    // Create transaction history record in Histories table
                    var historyRecord = new HistoryModel
                    {
                        HistoryID = 0, // Auto-generated by database
                        UserID = user.UserID,
                        Type = request.Action.ToLower(),
                        Amount = (float)request.SavingsAmount,
                        created_at = DateTime.UtcNow
                    };

                    Console.WriteLine($"Attempting to create history record: UserID={user.UserID}, Type={request.Action.ToLower()}, Amount={request.SavingsAmount}");
                    var historySuccess = await _supabaseService.CreateHistoryAsync(historyRecord);
                    Console.WriteLine($"Test history insertion result: {historySuccess}");

                    if (historySuccess)
                    {
                        return (true, $"₱{request.SavingsAmount:F2} successfully {actionText}. New balance: ₱{newBalance:F2}");
                    }
                    else
                    {
                        // Transaction was updated but history failed - still return success for the transaction
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

        // Simple test function to verify Histories insertion
        public async Task<bool> TestHistoryInsertionAsync(string userEmail, decimal amount, string action)
        {
            try
            {
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                    return false;

                var historyRecord = new HistoryModel
                {
                    HistoryID = 0,
                    UserID = user.UserID,
                    Type = action,
                    Amount = (float)amount,
                    created_at = DateTime.UtcNow
                };

                Console.WriteLine($"Testing history insertion: {JsonSerializer.Serialize(historyRecord)}");
                var result = await _supabaseService.CreateHistoryAsync(historyRecord);
                Console.WriteLine($"Test history insertion result: {result}");
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test history insertion error: {ex.Message}");
                return false;
            }
        }
    }
} 