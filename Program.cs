using Microsoft.AspNetCore.Authentication.Cookies;
using VotingSystem.Data;
using VotingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register MongoDB context
builder.Services.AddSingleton<MongoDbContext>();

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ElectionService>();
builder.Services.AddScoped<VotingService>();
builder.Services.AddScoped<VerificationService>();

// Add HttpContextAccessor for audit logging
builder.Services.AddHttpContextAccessor();

// Configure cookie-based authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "VotingSystem.Auth";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Use Always in production with HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Initialize MongoDB indexes
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await dbContext.InitializeIndexesAsync();
    Console.WriteLine("MongoDB indexes initialized successfully.");

    // Seed initial admin user if not exists
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    await SeedData.SeedAdminUserAsync(dbContext, authService);
    Console.WriteLine("Seed data initialized.");

    // OPTION 1: Small sample data (2 students, 1 election)
    // Uncomment to enable:
    // var electionService = scope.ServiceProvider.GetRequiredService<ElectionService>();
    // await SeedData.SeedSampleDataAsync(dbContext, authService, electionService);

    // OPTION 2: COMPREHENSIVE DATASET (100 students, 3 elections, candidates, votes)
    // Uncomment to enable:
    var electionService = scope.ServiceProvider.GetRequiredService<ElectionService>();
    await ComprehensiveSeedData.SeedLargeDatasetAsync(dbContext, authService, electionService);
}

// Configure the HTTP request pipeline
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
