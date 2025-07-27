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
            var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return false;

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? "";

            // Add debugging
            Console.WriteLine($"Google login - Email: {email}, Name: {name}");

            var existingUser = await _userRepository.GetUserByEmail(email);

            if (existingUser == null && email != null)
            {
                try
                {
                    var user = new Users
                    {
                        // UserID = Guid.NewGuid(),
                        Username = name ?? email,
                        Email = email,
                        created_at = DateTime.UtcNow
                    };
                 await _userRepository.CreateUser(user);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error inserting user: " + ex.Message);
                }
            }

            // Re-create claims and sign in manually
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, email ?? ""),
                    new Claim(ClaimTypes.Name, name ?? ""),
                    new Claim(ClaimTypes.Email, email ?? "")
                };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            //  Set session
            context.Session.SetString("name", name ?? string.Empty);

            //  Sign in
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(1)
                });

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