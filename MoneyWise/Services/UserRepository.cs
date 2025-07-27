using MoneyWise.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyWise.Services
{
    public class UserRepository
    {
        private readonly SupabaseService _supabaseService;

        public UserRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<List<Users>> GetAllUsers()
        {
            return await _supabaseService.GetAllUsersAsync();
        }

        public async Task<Users?> GetUserByEmail(string email)
        {
            return await _supabaseService.GetUserByEmailAsync(email);
        }

        public async Task<bool> CreateUser(Users user)  
        {
            return await _supabaseService.CreateUserAsync(user);
        }

        public async Task<bool> UpdateUser(int id, Users user)
        {
            return await _supabaseService.UpdateUserAsync(id, user);
        }

        public async Task<bool> DeleteUser(int id)
        {
            return await _supabaseService.DeleteUserAsync(id);
        }
    }
}