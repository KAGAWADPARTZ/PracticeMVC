using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using MoneyWise.Models;

namespace MoneyWise.Services
{
    public class FacebookTokenModel
    {
        public string? AccessToken { get; set; }
    }

    public class FacebookAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserRepository _userRepository;

        public FacebookAuthService(IHttpContextAccessor httpContextAccessor, UserRepository userRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
        }

        public async Task<bool> HandleFacebookLoginAsync(FacebookTokenModel model)
        {
            if (string.IsNullOrWhiteSpace(model.AccessToken))
                return false;

            // create a new http client
            using var client = new HttpClient();
            var fbResponse = await client.GetAsync(
                $"https://graph.facebook.com/me?fields=id,name,email,picture.width(100).height(100)&access_token={model.AccessToken}");

            if (!fbResponse.IsSuccessStatusCode)
                return false;

            // get the response from the facebook api
            var json = await fbResponse.Content.ReadAsStringAsync();
            dynamic fbUser = JsonConvert.DeserializeObject(json) ?? " ";

            string facebookId = fbUser.id;
            string name = fbUser.name;
            string email = fbUser.email;
            string pictureUrl = fbUser.picture.data.url;

            // Check if user already exists in the database by email
            var users = await _userRepository.GetAllUsers();
            var existingUser = users.FirstOrDefault(u => u.Email != null && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (existingUser == null)
            {
                // Insert user into Supabase only if not exists
                var user = new Users
                {
                    Username = name,
                    Email = email
                };
              await _userRepository.CreateUser(user);
            }

            // create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, facebookId),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email),
                new Claim("FacebookAccessToken", model.AccessToken),
                new Claim("ProfilePicture", pictureUrl)
            };
            // get user cookies and identity
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            //check if the httpcontext is null
           if (_httpContextAccessor.HttpContext == null) return false;

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddSeconds(60)
                });

            _httpContextAccessor.HttpContext.Session.SetString("name", name ?? string.Empty);

            return true;
        }
    }
}