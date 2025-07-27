using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MoneyWise.Services;
using System.Security.Claims;
using System.Xml.Linq;
using MoneyWise.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseNpgsql(connectionString)); // Use UseSqlServer for SQL Server

// Load appsettings.json, secrets, env vars, etc.
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(); // Optional, for dev

builder.Services.AddControllersWithViews();

//  Required to support session state
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Make the session cookie essential
});


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = false; // Optional: renew the cookie on each request
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var expiresUtc = context.Properties.ExpiresUtc;
                var currentUtc = DateTimeOffset.UtcNow;

                if (expiresUtc.HasValue && expiresUtc.Value < currentUtc)
                {
                    // Cookie is expired - sign out
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
// builder.Services.AddSingleton<MoneyWise.Services.DatabaseService>();
builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<FacebookAuthService>();
builder.Services.AddScoped<SavingsService>();
builder.Services.AddScoped<SavingsCalculatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

//cookies and session middleware
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();


