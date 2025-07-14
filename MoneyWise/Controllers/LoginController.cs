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

    }
}