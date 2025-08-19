using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotesSampleApplication.Data;
using NotesSampleApplication.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Connect to database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Add Identity (with ApplicationUser)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

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
