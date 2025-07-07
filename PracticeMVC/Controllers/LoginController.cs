using Microsoft.AspNetCore.Mvc;
using PracticeMVC.Models;


namespace PracticeMVC.Controllers
{
    
    public class LoginController : Controller
    {

    
        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }
        [HttpGet]
        [HttpPost]
        public IActionResult Index(string username, string password, bool rememberMe)
        {
            
            var verifier = new LoginVerification();
            if(verifier.PasswordChecker(username, password))
            {
                HttpContext.Session.SetString("username", username);
                return RedirectToAction("Index", "Home");

            }
            else
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }
        }
    }
}