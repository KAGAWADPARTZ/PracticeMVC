using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
        private readonly FacebookAuthService _facebookAuthService;

        public LoginController(ILogger<LoginController> logger, LoginService loginService, FacebookAuthService facebookAuthService)
        {
            _logger = logger;
            _loginService = loginService;
            _facebookAuthService = facebookAuthService;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var sessionName = HttpContext.Session.GetString("name");

            if (isAuthenticated && string.IsNullOrEmpty(sessionName))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return View(); // Stay on login page
            }

            if (isAuthenticated)
            {

                return RedirectToAction("Index", "Home");
            }

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
                ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(60) // Set cookie expiration
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
            var result = await _facebookAuthService.HandleFacebookLoginAsync(model);
            return Json(new { success = result });
        }

    }
}