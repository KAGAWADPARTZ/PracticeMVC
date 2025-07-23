using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoneyWise.Controllers
{
    [Route("api/session")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        [HttpGet("validate")]
        public IActionResult Validate()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                return Ok(new { valid = true });
            }

            return Ok(new { valid = false });
        }
    }
}
