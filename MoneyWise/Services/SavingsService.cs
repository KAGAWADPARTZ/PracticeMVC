using MoneyWise.Models;
using System.Text.Json;
using System.Text;

namespace MoneyWise.Services
{
    public class SavingsService
    {
        private readonly SupabaseService _supabaseService;
        private readonly UserRepository _userRepository;

        public SavingsService(SupabaseService supabaseService, UserRepository userRepository)
        {
            _supabaseService = supabaseService;
            _userRepository = userRepository;
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

                // Get savings from Supabase
                return await _supabaseService.GetSavingsByUserIdAsync(user.UserID);
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

                // Step 1: Get existing savings record
                var existingSavings = await _supabaseService.GetSavingsByUserIdAsync(user.UserID);

                if (existingSavings != null)
                {
                    // Step 2: Update the existing record
                    existingSavings.Amount += request.Action == "deposit" ? request.SavingsAmount : -request.SavingsAmount;
                    existingSavings.updated_at = DateTime.UtcNow;

                    var updateSuccess = await _supabaseService.UpdateSavingsAsync(existingSavings.TransactionID, existingSavings);

                    if (updateSuccess)
                    {
                        var actionText = request.Action == "deposit" ? "deposited" : "withdrawn";
                        return (true, $"₱{request.SavingsAmount:F2} successfully {actionText}.");
                    }
                    else
                    {
                        return (false, "Failed to update savings.");
                    }
                }
                else
                {
                    // Step 3: Insert new savings record
                    var newSavings = new Savings
                    {
                        UserID = user.UserID,
                        Amount = request.Action == "deposit" ? request.SavingsAmount : -request.SavingsAmount,
                        created_at = DateTime.UtcNow
                    };

                    var createSuccess = await _supabaseService.CreateSavingsAsync(newSavings);

                    if (createSuccess)
                    {
                        var actionText = request.Action == "deposit" ? "deposited" : "withdrawn";
                        return (true, $"₱{request.SavingsAmount:F2} successfully {actionText}.");
                    }
                    else
                    {
                        return (false, "Failed to create savings record.");
                    }
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