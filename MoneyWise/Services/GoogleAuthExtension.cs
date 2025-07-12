using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace MoneyWise.Services
{
    public static class GoogleAuthExtension
    {
        public static IServiceCollection AddGoogleAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "Google";
                })
                .AddCookie()
                .AddOAuth("Google", options =>
                {
                    options.ClientId = configuration["Authentication:Google:ClientId"] ?? "Null";
                    options.ClientSecret = configuration["Authentication:Google:ClientSecret"] ?? "Null";
                    options.CallbackPath = new PathString("/signin-google");

                    options.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
                    options.TokenEndpoint = "https://oauth2.googleapis.com/token";
                    options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";

                    options.Scope.Add("email");
                    options.Scope.Add("profile");

                    options.SaveTokens = true;

                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                    options.ClaimActions.MapJsonKey(ClaimTypes.StreetAddress, "address");
                    options.ClaimActions.MapJsonKey(ClaimTypes.DateOfBirth, "dateofbirth");

                    options.Events.OnCreatingTicket = async context =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                        var response = await context.Backchannel.SendAsync(request);
                        var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

                        context.RunClaimActions(user);
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
