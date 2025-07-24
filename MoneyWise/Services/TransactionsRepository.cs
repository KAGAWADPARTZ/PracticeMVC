using System.Text;
using System.Text.Json;
using MoneyWise.Models;

namespace MoneyWise.Services
{
    public class TransactionService
    {
        private readonly HttpClient _httpClient;
        private const string SupabaseUrl = "https://gzkgebvtemfidnwneqqt.supabase.co";
        private const string SupabaseApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imd6a2dlYnZ0ZW1maWRud25lcXF0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTE5NTQ3NTIsImV4cCI6MjA2NzUzMDc1Mn0.YysxMOrj_Rv3m9VLqQk9BFPOQOY9yhJNIBV8-jwlxYA";

        public TransactionService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(SupabaseUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("apikey", SupabaseApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseApiKey}");
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(int userId)
        {
            var response = await _httpClient.GetAsync($"/rest/v1/Transactions?UserID=eq.{userId}&select=*");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int transactionId)
        {
            var response = await _httpClient.GetAsync($"/rest/v1/Transactions?TransactionID=eq.{transactionId}&select=*");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Transaction>>(json)?.FirstOrDefault();
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
            var response = await _httpClient.DeleteAsync($"/rest/v1/Transactions?TransactionID=eq.{id}");
            return response.IsSuccessStatusCode;
        }
    }
}