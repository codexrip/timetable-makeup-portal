using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TimetableMakeup.Components;
using TimetableMakeup.Data;
using TimetableMakeup.Data.Seed;
using TimetableMakeup.Services;
using TimetableMakeup.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Interactive Server services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure MongoDB via MongoDB.EntityFrameworkCore
var mongoConnection = builder.Configuration["MongoDbSettings:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabase = builder.Configuration["MongoDbSettings:DatabaseName"] ?? "TimetableMakeupDb";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMongoDB(mongoConnection, mongoDatabase));

// Cookie authentication & authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Application services
builder.Services.AddScoped<IAuthService, HardcodedAuthService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<TimetableService>();
builder.Services.AddScoped<MakeupAvailabilityService>();
builder.Services.AddScoped<SlotSuggestionService>();
builder.Services.AddScoped<MakeupService>();

var app = builder.Build();

// Seed demo data on startup if collections are empty
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await TimetableSeeder.SeedAsync(db, logger);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not seed MongoDB on startup. Ensure MongoDB is running at {ConnectionString}.", mongoConnection);
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Cookie sign-out endpoint invoked by TopBar logout form
app.MapPost("/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
