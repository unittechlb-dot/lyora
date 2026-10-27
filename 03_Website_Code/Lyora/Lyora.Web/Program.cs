using Lyora.BLL.Services;
using Lyora.DAL;
using Lyora.DAL.Repositories;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;

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
builder.Services.AddScoped<CartRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<RawMaterialRepository>();
builder.Services.AddScoped<QuestionnaireRepository>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<PromotionRepository>();
builder.Services.AddScoped<AnalyticsRepository>();

// ---- Business Logic Layer ----
builder.Services.AddScoped<PerfumeService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<AdminOrderService>();
builder.Services.AddScoped<QuestionnaireService>();
builder.Services.AddScoped<RecommendationEngine>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<AnalyticsService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Numbers and dates always use the same format (1.5 – not 1,5), whatever the Windows language of the laptop.
var english = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(english),
    SupportedCultures = new[] { english },
    SupportedUICultures = new[] { english }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
