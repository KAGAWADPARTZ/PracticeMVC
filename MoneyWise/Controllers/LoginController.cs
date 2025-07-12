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
        private readonly UserRepository _userRepository;

        public LoginController(ILogger<LoginController> logger, UserRepository userRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
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
        //[HttpGet]
        //[HttpPost]

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
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
                return RedirectToAction("Index");
            var givenname = authenticateResult.Principal.FindFirst(ClaimTypes.GivenName)?.Value;
            var email = authenticateResult.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = authenticateResult.Principal.FindFirst(ClaimTypes.Name)?.Value;
            var streetaddress = authenticateResult.Principal.FindFirst(ClaimTypes.StreetAddress)?.Value;
            var dateofbirth = authenticateResult.Principal.FindFirst(ClaimTypes.DateOfBirth)?.Value;
            var contactnumber = authenticateResult.Principal.FindFirst(ClaimTypes.MobilePhone)?.Value;
            
            // Save data to session if needed
           
            var existingUser = _userRepository.GetAllUsers().FirstOrDefault(u => u.Username == email);
            if (existingUser == null)
            {
                var user = new MoneyWise.Models.Users
                {
                    Username = name ?? "",
                    Email = email ?? "",
                    ContactNumber = contactnumber ?? "",
                    Address = streetaddress ?? "",
                    created_at = null
                };
                _userRepository.CreateUser(user);
            }
            HttpContext.Session.SetString("name", name ?? "");

            return RedirectToAction("Index", "Home");
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