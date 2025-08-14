using MoneyWise.Models;

namespace MoneyWise.Services
{
    public class HistoryService
    {
        private readonly SupabaseService _supabaseService;
        private readonly UserRepository _userRepository;

        public HistoryService(SupabaseService supabaseService, UserRepository userRepository)
        {
            _supabaseService = supabaseService;
            _userRepository = userRepository;
        }

        public async Task<List<HistoryModel>> GetUserHistoryAsync(string userEmail)
        {
            try
            {
                // Get user from database
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);
                
                if (user == null)
                {
                    return new List<HistoryModel>();
                }

                // Get all history records for the user
                var histories = await _supabaseService.GetHistoriesByUserIdAsync(user.UserID);
                return histories ?? new List<HistoryModel>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetUserHistoryAsync: {ex.Message}");
                return new List<HistoryModel>();
            }
        }

        public async Task<bool> CreateHistoryRecordAsync(string userEmail, string type, decimal amount)
        {
            try
            {
                if (string.IsNullOrEmpty(userEmail))
                    return false;

                // Get user from database
                var users = await _userRepository.GetAllUsers();
                var user = users.FirstOrDefault(u => u.Email == userEmail);

                if (user == null)
                    return false;

                // Create history record
                var historyRecord = new HistoryModel
                {
                    HistoryID = 0, // Let database auto-generate the primary key
                    UserID = user.UserID,
                    Type = type,
                    Amount = amount,
                    created_at = DateTime.UtcNow
                };

                var success = await _supabaseService.CreateHistoryAsync(historyRecord);
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in CreateHistoryRecordAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<HistoryModel>> GetRecentHistoryAsync(string userEmail, int count = 10)
        {
            try
            {
                var allHistory = await GetUserHistoryAsync(userEmail);
                return allHistory.Take(count).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetRecentHistoryAsync: {ex.Message}");
                return new List<HistoryModel>();
            }
        }

        public async Task<List<HistoryModel>> GetHistoryByDateRangeAsync(string userEmail, DateTime startDate, DateTime endDate)
        {
            try
            {
                var allHistory = await GetUserHistoryAsync(userEmail);
                return allHistory.Where(h => h.created_at >= startDate && h.created_at <= endDate).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetHistoryByDateRangeAsync: {ex.Message}");
                return new List<HistoryModel>();
            }
        }

        public async Task<Dictionary<string, decimal>> GetHistorySummaryAsync(string userEmail)
        {
            try
            {
                var allHistory = await GetUserHistoryAsync(userEmail);
                
                var summary = new Dictionary<string, decimal>
                {
                    ["total_deposits"] = 0,
                    ["total_withdrawals"] = 0,
                    ["net_amount"] = 0
                };

                foreach (var record in allHistory)
                {
                    if (record.Type.ToLower() == "deposit")
                    {
                        summary["total_deposits"] += record.Amount;
                        summary["net_amount"] += record.Amount;
                    }
                    else if (record.Type.ToLower() == "withdrawal")
                    {
                        summary["total_withdrawals"] += record.Amount;
                        summary["net_amount"] -= record.Amount;
                    }
                }

                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetHistorySummaryAsync: {ex.Message}");
                return new Dictionary<string, decimal>
                {
                    ["total_deposits"] = 0,
                    ["total_withdrawals"] = 0,
                    ["net_amount"] = 0
                };
            }
        }
    }
}

