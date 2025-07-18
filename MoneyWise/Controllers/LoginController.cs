using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Models;
using MoneyWise.Services;
using System.Security.Claims;

namespace MoneyWise.Controllers
{

    public class LoginController : Controller
    {

        private readonly ILogger<LoginController> _logger;
        private readonly LoginService _loginService;

        public LoginController(ILogger<LoginController> logger, LoginService loginService)
        {
            _logger = logger;
            _loginService = loginService;
        }
        public IActionResult Index()
        {
            return View();
        }

        // Redirect to Google login
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Login");
            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) // Set cookie expiration
            };
            return Challenge(properties, "Google");
        }

        // Callback after Google login
        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await _loginService.HandleGoogleLoginAsync();
            _logger.LogInformation("Google login result: {Result}", result);
            return RedirectToAction(result ? "Index" : "Index", "Home");
        }

        // Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _loginService.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }

        [HttpPost]
        public async Task<IActionResult> FacebookCallback([FromBody] FacebookTokenModel model)
        {
            if (string.IsNullOrWhiteSpace(model.AccessToken))
                return Json(new { success = false });

            using var client = new HttpClient();
            var fbResponse = await client.GetAsync($"https://graph.facebook.com/me?fields=id,name,email&access_token={model.AccessToken}");

            if (!fbResponse.IsSuccessStatusCode)
                return Json(new { success = false });

            var json = await fbResponse.Content.ReadAsStringAsync();
            dynamic fbUser = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            string facebookId = fbUser.id;
            string name = fbUser.name;
            string email = fbUser.email;

            // Create claims
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, facebookId),
        new Claim(ClaimTypes.Name, name),
        new Claim(ClaimTypes.Email, email),
        new Claim("FacebookAccessToken", model.AccessToken)
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddHours(2)
            });

            return Json(new { success = true });
        }

        public class FacebookTokenModel
        {
            public string AccessToken { get; set; }
        }

    }
}