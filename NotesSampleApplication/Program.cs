using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NotesSampleApplication.Data;
using NotesSampleApplication.Models;

var builder = WebApplication.CreateBuilder(args);

// 1.Connect to database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2.Add Identity (with ApplicationUser)
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// 3.Add MVC + Razor Pages
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

// ADD THIS - force signout on every app start
app.Use(async (context, next) =>
{
    if (context.User.Identity.IsAuthenticated)
    {
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
    }
    await next();
});
app.UseAuthorization();

// When unauthenticated, redirect to login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Notes}/{action=Index}/{id?}").RequireAuthorization();

app.MapRazorPages(); // Required for Identity




app.Run();
