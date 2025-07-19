using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace MoneyWise.Services
{
    public class FacebookTokenModel
    {
        public string AccessToken { get; set; }
    }

    public class FacebookAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FacebookAuthService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> HandleFacebookLoginAsync(FacebookTokenModel model)
        {
            if (string.IsNullOrWhiteSpace(model.AccessToken))
                return false;

            using var client = new HttpClient();
            var fbResponse = await client.GetAsync(
                $"https://graph.facebook.com/me?fields=id,name,email,picture.width(100).height(100)&access_token={model.AccessToken}");

            if (!fbResponse.IsSuccessStatusCode)
                return false;

            var json = await fbResponse.Content.ReadAsStringAsync();
            dynamic fbUser = JsonConvert.DeserializeObject(json);

            string facebookId = fbUser.id;
            string name = fbUser.name;
            string email = fbUser.email;
            string pictureUrl = fbUser.picture.data.url;

            var claims = new List<Claim>
    {
                new Claim(ClaimTypes.NameIdentifier, facebookId),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email),
                new Claim("FacebookAccessToken", model.AccessToken),
                new Claim("ProfilePicture", pictureUrl)
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(2)
                });

            return true;
        }
    }
}