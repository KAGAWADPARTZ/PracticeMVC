using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MoneyWise.Services;
using System.Security.Claims;
using MoneyWise.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//  Load secrets from environment/user-secrets/appsettings.json
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(); // Only needed during development

//  Use Supabase connection via environment variable
var supabaseUrl = builder.Configuration["Authentication:Supabase:Url"];
var supabaseApiKey = builder.Configuration["Authentication:Supabase:ApiKey"];
if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseApiKey))
{
    throw new InvalidOperationException("Supabase credentials are not configured.");
}

// ✅ Optionally use DB connection string from secrets
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)); // Or UseSqlServer, etc.

builder.Services.AddControllersWithViews();

// ✅ Session Setup
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ✅ Cookie Auth Setup
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = false;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var expiresUtc = context.Properties.ExpiresUtc;
                var currentUtc = DateTimeOffset.UtcNow;
                if (expiresUtc.HasValue && expiresUtc.Value < currentUtc)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<SessionValidationService>();
builder.Services.AddSingleton<SupabaseService>(); // will use IConfiguration internally
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<FacebookAuthService>();
builder.Services.AddScoped<SavingsService>();
builder.Services.AddScoped<SavingsCalculatorService>();
builder.Services.AddScoped<HistoryService>();

var app = builder.Build();

// Middleware & routing
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Cookie/session consistency check
app.Use(async (context, next) =>
{
    var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
    var sessionName = context.Session.GetString("name");

    if (isAuthenticated && string.IsNullOrEmpty(sessionName))
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Response.Redirect("/Login/Index");
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

// Catch-all route for unmatched URLs - redirect to 404
app.MapFallback(context =>
{
    context.Response.StatusCode = 404;
    context.Response.Redirect("/Error/404");
    return Task.CompletedTask;
});

app.Run();