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
            if (code == 404)
                return RedirectToAction("Error404");

            return View("GeneralError"); // Optional: create Views/Shared/GeneralError.cshtml
        }
    }
}
