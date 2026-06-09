using ApiDemo.Data;
using ApiDemo.Hubs;
using ApiDemo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ApiDemoBank.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddDbContext<BankingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BankingDb")));

builder.Services.AddSignalR();
builder.Services.AddScoped<CustomerNotificationService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar", options =>
    {
        options
            .WithTitle("ApiDemo Banking API")
            .WithTheme(ScalarTheme.Kepler)
            .WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });

    await DevelopmentDataSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.Use(async (context, next) =>
{
    var isInlineKycDocumentPreview =
        context.Request.Path.StartsWithSegments("/api/admin/kyc-documents")
        && context.Request.Path.Value?.EndsWith("/file", StringComparison.OrdinalIgnoreCase) == true
        && !context.Request.Query.ContainsKey("download");

    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    if (!isInlineKycDocumentPreview)
    {
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    }
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    if (!app.Environment.IsDevelopment())
    {
        var contentSecurityPolicy = isInlineKycDocumentPreview
            ? "default-src 'none'; frame-ancestors 'self' http://localhost:5173"
            : "default-src 'none'; frame-ancestors 'none'";
        context.Response.Headers.TryAdd("Content-Security-Policy", contentSecurityPolicy);
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");

app.Run();
