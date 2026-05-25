using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<MyAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

var redisConnectionString = builder.Configuration["ConnectionStrings:Redis"] ??  throw new NullReferenceException("Redis connection string not set");
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddScoped<RedisContext>();

// Add session services for cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Signin/Index";
        options.LogoutPath = "/Signin/Logout";
        options.AccessDeniedPath = "/Signin/AccessDenied"; 
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true; 
    });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<MyAppContext>();
var adminEmail = builder.Configuration["AdminCredentials:Email"] ?? throw new NullReferenceException("Admin email address not set");
var adminPassword = builder.Configuration["AdminCredentials:Password"] ?? throw new NullReferenceException("Admin password not set");

if (!dbContext.Employees.Any(x => x.Email == adminEmail))
{
    dbContext.Employees.Add(new Employee
    {
        Name = "Admin",
        CardNumber = "00-00-00",
        PhoneNumber = "0000000000",
        Email = adminEmail,
        Password = PasswordHelper.HashPassword(adminPassword),
        Role = "Admin",
        IsAvailable = "Available"
    });

    dbContext.SaveChanges();
}

app.Run();
