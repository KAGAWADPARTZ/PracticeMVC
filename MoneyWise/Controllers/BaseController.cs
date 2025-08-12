using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MoneyWise.Controllers
{
    [Authorize]
    public abstract class BaseController : Controller
    {
        protected readonly ILogger _logger;

        protected BaseController(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the current user's email from claims
        /// </summary>
        /// <returns>User email or null if not found</returns>
        protected string? GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Validates if the current user is authenticated
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        protected bool IsUserAuthenticated()
        {
            return !string.IsNullOrEmpty(GetCurrentUserEmail());
        }

        /// <summary>
        /// Creates a standardized JSON success response
        /// </summary>
        /// <param name="data">Data to include in response</param>
        /// <param name="message">Optional message</param>
        /// <returns>JsonResult</returns>
        protected JsonResult JsonSuccess(object data, string? message = null)
        {
            return Json(new { success = true, data, message });
        }

        /// <summary>
        /// Creates a standardized JSON error response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="data">Optional data</param>
        /// <returns>JsonResult</returns>
        protected JsonResult JsonError(string message, object? data = null)
        {
            return Json(new { success = false, message, data });
        }

        /// <summary>
        /// Creates a standardized JSON response for unauthenticated users
        /// </summary>
        /// <returns>JsonResult</returns>
        protected JsonResult JsonUnauthorized()
        {
            return JsonError("User not authenticated");
        }

        /// <summary>
        /// Handles exceptions and returns appropriate JSON response
        /// </summary>
        /// <param name="ex">Exception that occurred</param>
        /// <param name="operation">Description of the operation being performed</param>
        /// <returns>JsonResult</returns>
        protected JsonResult HandleException(Exception ex, string operation)
        {
            _logger.LogError(ex, "Error during {Operation}", operation);
            return JsonError($"An error occurred while {operation.ToLower()}");
        }

        /// <summary>
        /// Validates user authentication and returns appropriate response if not authenticated
        /// </summary>
        /// <returns>JsonResult if not authenticated, null if authenticated</returns>
        protected JsonResult? ValidateAuthentication()
        {
            if (!IsUserAuthenticated())
            {
                return JsonUnauthorized();
            }
            return null;
        }
    }
}
