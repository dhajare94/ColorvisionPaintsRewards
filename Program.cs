using Microsoft.AspNetCore.Authentication.Cookies;
using QRRewardPlatform.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Firebase & application services
builder.Services.AddSingleton<FirebaseService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<CampaignService>();
builder.Services.AddSingleton<RewardSlabService>();
builder.Services.AddSingleton<CodeService>();
builder.Services.AddSingleton<CustomerService>();
builder.Services.AddSingleton<RedemptionBatchService>();
builder.Services.AddSingleton<RedemptionService>();
builder.Services.AddSingleton<PayoutService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<QRCodeGeneratorService>();
builder.Services.AddSingleton<ImgBBService>();
builder.Services.AddSingleton<EnquiryService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebsitePolicy", policy =>
    {
        policy.AllowAnyOrigin() // Allow all for now, as domain is not specified
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Seed default admin on startup
using (var scope = app.Services.CreateScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    await authService.SeedDefaultAdminAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseCors("WebsitePolicy");

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
