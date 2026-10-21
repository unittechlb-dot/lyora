using Lyora.BLL.Services;
using Lyora.DAL;
using Lyora.DAL.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// ---- Login with a secure cookie (roles: Customer, Administrator) ----
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "Lyora.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// ---- Data Access Layer ----
var connectionString = builder.Configuration.GetConnectionString("LyoraDB")
    ?? throw new InvalidOperationException("Connection string 'LyoraDB' is missing in appsettings.json.");
builder.Services.AddSingleton(new Db(connectionString));
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<PerfumeRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<DashboardRepository>();

// ---- Business Logic Layer ----
builder.Services.AddScoped<PerfumeService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<DashboardService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
