using MarketRateInfo.Server;
using MarketRateInfo.Server.BusinessLogic.Configuration;
using MarketRateInfo.Server.DataAccess;
using MarketRateInfo.Server.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// Application paths
// -----------------------------------------------------------------------------

// Make relative paths such as Logs/... deterministic regardless of the
// directory from which dotnet/Visual Studio/Docker starts the application.
Directory.SetCurrentDirectory(builder.Environment.ContentRootPath);

Directory.CreateDirectory(
    Path.Combine(builder.Environment.ContentRootPath, "Logs"));

var appDataPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "App_Data");

Directory.CreateDirectory(appDataPath);

var databasePath = Path.Combine(appDataPath, "marketrateinfo.db");

// -----------------------------------------------------------------------------
// Logging
// -----------------------------------------------------------------------------

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// -----------------------------------------------------------------------------
// Services
// -----------------------------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["https://localhost:60028"];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-MarketRateInfo.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -----------------------------------------------------------------------------
// Database
// -----------------------------------------------------------------------------

builder.Services.AddDbContext<MarketRateDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

// -----------------------------------------------------------------------------
// Application services
// -----------------------------------------------------------------------------

builder.Services.AddApplicationServices(builder.Configuration);

// -----------------------------------------------------------------------------
// Build application
// -----------------------------------------------------------------------------

var app = builder.Build();

// -----------------------------------------------------------------------------
// Startup information
// -----------------------------------------------------------------------------

var providerOptions = app.Services
    .GetRequiredService<IOptions<MarketRateProviderOptions>>()
    .Value;

app.Logger.LogInformation(
    "MarketRateInfo.Server starting. Database: {DatabasePath}. ContentRoot: {ContentRoot}",
    databasePath,
    builder.Environment.ContentRootPath);

// -----------------------------------------------------------------------------
// Database initialization
// -----------------------------------------------------------------------------

await DatabaseInitializer.InitializeAsync(app.Services);

// -----------------------------------------------------------------------------
// Identity initialization / seeding
// -----------------------------------------------------------------------------

await using (var identityScope = app.Services.CreateAsyncScope())
{
    await identityScope.ServiceProvider
        .GetRequiredService<IdentitySeedService>()
        .SeedAsync();
}

// -----------------------------------------------------------------------------
// HTTP pipeline
// -----------------------------------------------------------------------------

// Global exception handling must be one of the first middleware components.
app.UseMiddleware<ErrorHandlingMiddleware>();

// Adds Serilog request logging.
app.UseSerilogRequestLogging();

// Adds security-related response headers.
app.UseHsts();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP -> HTTPS.
app.UseHttpsRedirection();

// Handles CORS before authentication/authorization.
app.UseCors("DefaultCorsPolicy");

// Validate antiforgery tokens on cookie-authenticated state-changing requests.
// The SPA obtains a request token from GET /api/auth/csrf before making mutations.
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var requiresAntiforgery = HttpMethods.IsPost(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method)
        || HttpMethods.IsDelete(method);

    if (requiresAntiforgery &&
        !context.Request.Path.Equals("/api/auth/csrf", StringComparison.OrdinalIgnoreCase))
    {
        var antiforgery = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid or missing antiforgery token." });
            return;
        }
    }

    await next();
});

// Authentication must execute before authorization.
app.UseAuthentication();
app.UseAuthorization();

// -----------------------------------------------------------------------------
// API endpoints
// -----------------------------------------------------------------------------

app.MapControllers();

// -----------------------------------------------------------------------------
// SPA static files
// -----------------------------------------------------------------------------

app.UseDefaultFiles();
app.MapStaticAssets();
app.MapFallbackToFile("index.html");

// -----------------------------------------------------------------------------
// Run
// -----------------------------------------------------------------------------
app.Run();

public partial class Program;