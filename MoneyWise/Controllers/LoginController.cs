using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MoneyWise.Models;
using System.Security.Claims;

namespace MoneyWise.Controllers
{
    
    public class LoginController : Controller
    {

        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }
        private IActionResult Register()
        {
            // Registration logic can be implemented here

            return View();
        }
        [HttpGet]
        [HttpPost]

        // Redirect to Google login
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Login");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Google");
        }

        // Callback after Google login
        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                return RedirectToAction("Index", "Home"); // or Login page
            }

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            // Save data to session if needed
            HttpContext.Session.SetString("username", name ?? "Unknown");

            // Optional: save user to your database

            return RedirectToAction("Index", "Home"); // Or dashboard
        }
        // Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Register", "Login");
        }
       
    }
}