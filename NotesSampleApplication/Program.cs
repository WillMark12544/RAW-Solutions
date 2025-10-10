using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotesSampleApplication.Data;
using NotesSampleApplication.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using NotesSampleApplication.Services;
using Microsoft.Extensions.FileProviders;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;


var builder = WebApplication.CreateBuilder(args);

// Make reader environment variables like .env for SA password
builder.Configuration.AddEnvironmentVariables();


// 1. Connect to database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Add Identity (with ApplicationUser)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.AllowedForNewUsers = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(0);
    options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
})

.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddTransient<IEmailSender, DummyEmailSender>();

// 3. Force session-only cookies (Forces Logout cookies)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = false;
    options.Cookie.MaxAge = null; // session-only

    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";

    // Ensure cookies are always non-persistent
    options.Events.OnSigningIn = context =>
    {
        context.Properties.IsPersistent = false;
        return Task.CompletedTask;
    };
});

// 4. Data protection: reset for every restart
builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// Error check to ensure roles do not try to seed before database has started
var retries = 5;
while (retries > 0)
{
    try
    {
        // Seed roles
        using (var scope = app.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = { "Normal", "Disabled", "Admin" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
        break;
    }
    catch (Exception ex)
    {
        retries--;
        if (retries == 0) throw;
        await Task.Delay(5000);
    }
}

// Seed test users VULNERABILITY 
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // INTENTIONAL VULNERABILITY: Hardcoded credentials in source code
    // Default admin password: 06g073rocnMVAuvQ0ZrovFMG8d237O
    string adminPassword = "06g073rocnMVAuvQ0ZrovFMG8d237O!";
    string adminEmail = "admin@example.com";

    // Seed Admin User
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, adminPassword);
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// INTENTIONAL VULNERABILITY: Hardcoded API credentials for security testing
var awsAccessKey = "AKJAIOSFODNN7EXAMPLE";
var awsSecretKey = "wJalrXUtnFRMI/K7MDENG/bPxRhiCYEXAMPLEKEY";
var stripeApiKey = "sk_live_51HqJ8dKZvKYlo2C8a1k2c3d4e5f6g7h8i9j0r1l2m3n4o5p6q7r8s9t0u1v2n3x4y5z6";


// check if upload directory exists
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

// create upload directory if does not exist
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    EnableDirectoryBrowsing = true
});

// Middleware pipeline?
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Default route (Notes controller requires login)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Notes}/{action=Index}/{id?}")
    .RequireAuthorization();

app.MapRazorPages(); // Required for Identity


app.Run(); //Test Push

