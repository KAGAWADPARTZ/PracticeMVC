using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MoneyWise.Services;

namespace MoneyWise.Controllers
{
    [Authorize]
    public abstract class BaseController : Controller
    {
        protected readonly ILogger _logger;
        protected readonly LoginService _loginService;

        protected BaseController(ILogger logger, LoginService loginService)
        {
            _logger = logger;
            _loginService = loginService;
        }

        /// <summary>
        /// Validates session before any action execution
        /// </summary>
        protected async Task<bool> ValidateSessionAsync()
        {
            try
            {
                var isValid = await _loginService.ValidateSessionAsync();
                if (!isValid)
                {
                    _logger.LogWarning("Session validation failed, user will be redirected to login");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session validation");
                return false;
            }
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
        /// Creates a standardized JSON response for expired sessions
        /// </summary>
        /// <returns>JsonResult</returns>
        protected JsonResult JsonSessionExpired()
        {
            return JsonError("Session has expired. Please log in again.");
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

        /// <summary>
        /// Validates session and returns appropriate response if expired
        /// </summary>
        /// <returns>JsonResult if session expired, null if valid</returns>
        protected async Task<JsonResult?> ValidateSessionAndReturnResponseAsync()
        {
            var isValid = await ValidateSessionAsync();
            if (!isValid)
            {
                return JsonSessionExpired();
            }
            return null;
        }

        /// <summary>
        /// Gets remaining session time for display purposes
        /// </summary>
        /// <returns>TimeSpan representing remaining session time</returns>
        protected TimeSpan GetRemainingSessionTime()
        {
            return _loginService.GetRemainingSessionTime();
        }

        /// <summary>
        /// Checks if session is about to expire soon
        /// </summary>
        /// <returns>True if session expires within 5 minutes</returns>
        protected bool IsSessionExpiringSoon()
        {
            return _loginService.IsSessionExpiringSoon();
        }
    }
}
