using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MoneyWise.Services;
using System.Security.Claims;
using System.Xml.Linq;
using MoneyWise.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)); // Use UseSqlServer for SQL Server


builder.Services.AddControllersWithViews();

// 🔥 Required to support session state
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Make the session cookie essential
});


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(2);
        options.SlidingExpiration = false; // Optional: renew the cookie on each request
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });


builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LoginService>();
builder.Services.AddSingleton<MoneyWise.Services.DatabaseService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<FacebookAuthService>();

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

//username session
app.Use(async (context, next) =>
{
    if (context.User.Identity.IsAuthenticated && string.IsNullOrEmpty(context.Session.GetString("name")))
    {
        var name = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        context.Session.SetString("name", name ?? string.Empty);
    }
    await next();
}); 

app.UseAuthentication();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();


