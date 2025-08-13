using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace MoneyWise.Services
{
    public class SessionValidationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionValidationService> _logger;

        public SessionValidationService(IHttpContextAccessor httpContextAccessor, ILogger<SessionValidationService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Validates if the current session is still valid
        /// </summary>
        /// <returns>True if session is valid, false if expired</returns>
        public async Task<bool> ValidateSessionAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return false;

            try
            {
                // Check if user is authenticated
                var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
                if (!isAuthenticated) return false;

                // Check if session exists
                var sessionName = context.Session.GetString("name");
                if (string.IsNullOrEmpty(sessionName))
                {
                    _logger.LogWarning("Session validation failed: No session name found for authenticated user");
                    await ForceLogoutAsync();
                    return false;
                }

                // Check if session has expired (1 hour timeout)
                var sessionCreated = context.Session.GetString("session_created");
                if (!string.IsNullOrEmpty(sessionCreated))
                {
                    if (DateTime.TryParse(sessionCreated, out var createdTime))
                    {
                        var sessionTimeout = TimeSpan.FromHours(1);
                        if (DateTime.UtcNow - createdTime > sessionTimeout)
                        {
                            _logger.LogWarning("Session validation failed: Session has expired");
                            await ForceLogoutAsync();
                            return false;
                        }
                    }
                }

                // Check if cookie has expired
                var authenticateResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (!authenticateResult.Succeeded)
                {
                    _logger.LogWarning("Session validation failed: Cookie authentication failed");
                    await ForceLogoutAsync();
                    return false;
                }

                // Check if cookie expiration time has passed
                var expiresUtc = authenticateResult.Properties?.ExpiresUtc;
                if (expiresUtc.HasValue && expiresUtc.Value < DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("Session validation failed: Cookie has expired");
                    await ForceLogoutAsync();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session validation");
                await ForceLogoutAsync();
                return false;
            }
        }

        /// <summary>
        /// Forces logout by clearing session and authentication cookies
        /// </summary>
        public async Task ForceLogoutAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            try
            {
                // Clear session
                context.Session.Clear();

                // Sign out from authentication
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("User forcefully logged out due to session/cookie expiration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during force logout");
            }
        }

        /// <summary>
        /// Sets session creation time and validates session on login
        /// </summary>
        public void SetSessionCreated()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            context.Session.SetString("session_created", DateTime.UtcNow.ToString("O"));
        }

        /// <summary>
        /// Gets the remaining session time
        /// </summary>
        /// <returns>TimeSpan representing remaining session time, or TimeSpan.Zero if expired</returns>
        public TimeSpan GetRemainingSessionTime()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return TimeSpan.Zero;

            var sessionCreated = context.Session.GetString("session_created");
            if (string.IsNullOrEmpty(sessionCreated)) return TimeSpan.Zero;

            if (DateTime.TryParse(sessionCreated, out var createdTime))
            {
                var sessionTimeout = TimeSpan.FromHours(1);
                var elapsed = DateTime.UtcNow - createdTime;
                var remaining = sessionTimeout - elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }

            return TimeSpan.Zero;
        }

        /// <summary>
        /// Checks if session is about to expire (within 5 minutes)
        /// </summary>
        /// <returns>True if session expires within 5 minutes</returns>
        public bool IsSessionExpiringSoon()
        {
            var remaining = GetRemainingSessionTime();
            return remaining <= TimeSpan.FromMinutes(5) && remaining > TimeSpan.Zero;
        }
    }
}
