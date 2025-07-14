using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using MoneyWise.Models;
using System.Security.Claims;
namespace MoneyWise.Services
{
    public class LoginService
    {
        private readonly UserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginService(UserRepository userRepository, IHttpContextAccessor httpContextAccessor)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> HandleGoogleLoginAsync()
        {
            var context = _httpContextAccessor.HttpContext!;
            var authresult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authresult.Succeeded)
                return false;

            var email = authresult.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = authresult.Principal.FindFirst(ClaimTypes.Name)?.Value;
            Console.WriteLine("Email from Google: " + email);
            Console.WriteLine("Name from Google: " + name);


            var existingUser = _userRepository.GetAllUsers().FirstOrDefault(u => u.Email == email);

            Console.WriteLine("🟢 Google login callback triggered");

            if (existingUser == null && email != null)
            {
                Console.WriteLine("❌ No email found");
                try
                {
                    var user = new Users
                    {
                        Username = name ?? email,
                        Email = email
                    };
                    _userRepository.CreateUser(user);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error inserting user: " + ex.Message);
                }
            }

            context.Session.SetString("name", name ?? "");

            return true;
        }

        public async Task SignOutAsync()
        {
            var context = _httpContextAccessor.HttpContext;

            if (context != null) // Ensure context is not null before calling SignOutAsync
            {
                try
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                Console.WriteLine("HttpContext is null. Unable to sign out.");
            }
        }
    }

}