using System.Text;
using System.Text.Json;
using MoneyWise.Models;

namespace MoneyWise.Services
{
    public class SupabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseApiKey;

        public SupabaseService(IConfiguration configuration)
        {
            _supabaseUrl = configuration["Authentication:Supabase:Url"]!;
            _supabaseApiKey = configuration["Authentication:Supabase:ApiKey"]!;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_supabaseUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // Debug logs
            Console.WriteLine($"Supabase URL: {_supabaseUrl}");
            Console.WriteLine($"Supabase API Key: {_supabaseApiKey.Substring(0, 10)}...");
        }

        public async Task<List<Users>> GetAllUsersAsync()
        {
            try
            {
                Console.WriteLine("Calling Supabase API to get all users...");
                var response = await _httpClient.GetAsync("/rest/v1/Users?select=*");
                
                Console.WriteLine($"Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Supabase error: {errorContent}");
                    return new List<Users>();
                }
                
                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response JSON: {json}");
                
                var users = JsonSerializer.Deserialize<List<Users>>(json);
                Console.WriteLine($"Deserialized users count: {users?.Count ?? 0}");
                
                return users ?? new List<Users>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAllUsersAsync: {ex.Message}");
                return new List<Users>();
            }
        }

       public async Task<Users?> GetUserByEmailAsync(string email)
        {
            try
            {
                Console.WriteLine($"Looking for user with email: {email}");
                var encodedEmail = Uri.EscapeDataString(email);
                var url = $"/rest/v1/Users?Email=eq.{encodedEmail}&select=*";
                Console.WriteLine($"Supabase URL: {url}");
                
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Supabase error: {errorContent}");
                    return null;
                }
                
                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response JSON: {json}");
                
                var users = JsonSerializer.Deserialize<List<Users>>(json);
                var user = users?.FirstOrDefault();
                Console.WriteLine($"Found user: {user?.Email}");
                
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetUserByEmailAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateUserAsync(Users user)
        {
            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/rest/v1/Users", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateUserAsync(int id, Users user)
        {
            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/rest/v1/Users?UserID=eq.{id}")
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"/rest/v1/Users?UserID=eq.{id}");
            return response.IsSuccessStatusCode;
        }

        // Savings methods
        // Savings methods
        public async Task<List<Savings>> GetAllSavingsAsync()
        {
            var response = await _httpClient.GetAsync("/rest/v1/Savings?select=*");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Savings>>(json) ?? new List<Savings>();
        }

        public async Task<Savings?> GetSavingsByUserIdAsync(int userId)  // Changed parameter type
        {
            var response = await _httpClient.GetAsync($"/rest/v1/Savings?UserID=eq.{userId}&select=*&order=created_at.desc&limit=1");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            var json = await response.Content.ReadAsStringAsync();
            var savings = JsonSerializer.Deserialize<List<Savings>>(json);
            return savings?.FirstOrDefault();
        }

        public async Task<bool> CreateSavingsAsync(Savings savings)
        {
            var json = JsonSerializer.Serialize(savings);
            Console.WriteLine($"JSON being sent to Supabase: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/rest/v1/Savings", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response status: {response.StatusCode}");
            Console.WriteLine($"Response body: {responseContent}");

            return response.IsSuccessStatusCode;
        }


        public async Task<bool> UpdateSavingsAsync(int savingsId, Savings savings)
        {
            var json = JsonSerializer.Serialize(savings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/rest/v1/Savings?TransactionID=eq.{savingsId}")
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        // Transaction methods for savings calculator
        public async Task<List<Savings>?> GetTransactionsByUserIdAsync(int userId)  // Changed parameter type
        {
            var response = await _httpClient.GetAsync($"/rest/v1/Savings?UserID=eq.{userId}&select=*&order=created_at.desc");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Savings>>(json);
        }

        // History methods
        public async Task<List<HistoryModel>> GetAllHistoriesAsync()
        {
            var response = await _httpClient.GetAsync("/rest/v1/Histories?select=*");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<HistoryModel>>(json) ?? new List<HistoryModel>();
        }

        public async Task<List<HistoryModel>> GetHistoriesByUserIdAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"/rest/v1/Histories?UserID=eq.{userId}&select=*&order=created_at.desc");
            if (!response.IsSuccessStatusCode)
            {
                return new List<HistoryModel>();
            }
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<HistoryModel>>(json) ?? new List<HistoryModel>();
        }

        public async Task<bool> CreateHistoryAsync(HistoryModel history)
        {
            var json = JsonSerializer.Serialize(history);
            Console.WriteLine($"JSON being sent to Histories table: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/rest/v1/Histories", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Histories response status: {response.StatusCode}");
            Console.WriteLine($"Histories response body: {responseContent}");

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateHistoryAsync(int historyId, HistoryModel history)
        {
            var json = JsonSerializer.Serialize(history);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/rest/v1/Histories?HistoryID=eq.{historyId}")
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteHistoryAsync(int historyId)
        {
            var response = await _httpClient.DeleteAsync($"/rest/v1/Histories?HistoryID=eq.{historyId}");
            return response.IsSuccessStatusCode;
        }
     }
}
