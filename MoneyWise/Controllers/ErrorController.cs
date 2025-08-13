using Microsoft.AspNetCore.Mvc;

namespace MoneyWise.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/404")]
        public IActionResult Error404()
        {
            return View("NotFound");
        }

        [Route("Error/{code}")]
        public IActionResult GeneralError(int code)
        {
            // Redirect all error codes to the 404 page
            return RedirectToAction("Error404");
        }
    }
}
