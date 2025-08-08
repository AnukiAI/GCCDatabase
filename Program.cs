using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ===== SESSION CONFIGURATION =====
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "GCC.Session";
});

// ===== DISTRIBUTED CACHE =====
builder.Services.AddDistributedMemoryCache();

// ===== COOKIE AUTHENTICATION FOR TESTING =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "GCC.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// ===== AUTHORIZATION POLICIES =====
builder.Services.AddAuthorization(options =>
{
    // Role-based policies for testing - simplified but functional
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("Role", "Admin"));

    options.AddPolicy("AnalystOrAdmin", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  context.User.HasClaim("Role", "Admin") ||
                  context.User.HasClaim("Role", "Analyst")));

    options.AddPolicy("ExternalAccess", policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context =>
                  context.User.HasClaim("Role", "External") ||
                  context.User.HasClaim("Role", "Admin") ||
                  context.User.HasClaim("Role", "Analyst")));

    options.AddPolicy("AuthenticatedUsers", policy =>
        policy.RequireAuthenticatedUser());

    // No fallback policy for testing - allow anonymous by default
});

// ===== MVC & RAZOR PAGES CONFIGURATION =====
builder.Services.AddControllersWithViews(options =>
{
    // Remove global authorization filter for testing
    // Individual controllers can use [Authorize] as needed
})
.AddJsonOptions(options =>
{
    // Configure JSON serialization for API responses
    options.JsonSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddRazorPages();

// ===== BASIC SERVICES =====
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// ===== BUILD APPLICATION =====
var app = builder.Build();

// ===== STARTUP LOGGING (before middleware) =====
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("GreenCircle Certified Database application starting up");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);

// ===== MIDDLEWARE PIPELINE CONFIGURATION =====

// Security headers (should be first)
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
    }

    await next();
});

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// HTTPS redirection
app.UseHttpsRedirection();

// Static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 7 days in production
        if (!app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
        }
    }
});

// Routing
app.UseRouting();

// Session management (before authentication)
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Role middleware - set session roles based on claims
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        // Get role from claims and store in session
        var roleClaim = context.User.FindFirst("Role")?.Value;
        if (!string.IsNullOrEmpty(roleClaim))
        {
            context.Session.SetString("UserRole", roleClaim);
        }

        // Get other user info
        var emailClaim = context.User.FindFirst("Email")?.Value;
        var nameClaim = context.User.FindFirst("Name")?.Value;

        if (!string.IsNullOrEmpty(emailClaim))
            context.Session.SetString("UserEmail", emailClaim);

        if (!string.IsNullOrEmpty(nameClaim))
            context.Session.SetString("UserName", nameClaim);
    }
    else
    {
        // Set default role for unauthenticated users
        context.Session.SetString("UserRole", "Public");
    }

    await next();
});

// ===== ROUTE MAPPING =====

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Area routes (if you have admin areas)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Razor Pages
app.MapRazorPages();

logger.LogInformation("Application started successfully");

// ===== RUN APPLICATION =====
app.Run();