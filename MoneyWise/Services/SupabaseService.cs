using System.Text;
using System.Text.Json;
using MoneyWise.Models;

namespace MoneyWise.Services
{
    public class SupabaseService
    {
        private readonly HttpClient _httpClient;
        private const string SupabaseUrl = "https://gzkgebvtemfidnwneqqt.supabase.co";
        private const string SupabaseApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imd6a2dlYnZ0ZW1maWRud25lcXF0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTE5NTQ3NTIsImV4cCI6MjA2NzUzMDc1Mn0.YysxMOrj_Rv3m9VLqQk9BFPOQOY9yhJNIBV8-jwlxYA";

        public SupabaseService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(SupabaseUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("apikey", SupabaseApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<Users>> GetAllUsersAsync()
        {
            var response = await _httpClient.GetAsync("/rest/v1/Users?select=*");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Users>>(json) ?? new List<Users>();
        }

        public async Task<Users?> GetUserByEmailAsync(string email)
        {
            var encodedEmail = Uri.EscapeDataString(email);
            var response = await _httpClient.GetAsync($"/rest/v1/Users?Email=eq.{encodedEmail}&select=*");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Supabase error {response.StatusCode}: {errorContent}");
            }
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<Users>>(json);
            return users?.FirstOrDefault();
        }

        public async Task<bool> CreateUserAsync(Users user)
        {
            var json = JsonSerializer.Serialize(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/rest/v1/Users", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateUserAsync(Guid id, Users user)
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

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"/rest/v1/Users?UserID=eq.{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
