using MarketRateInfo.Server.Akka;
using MarketRateInfo.Server.BusinessLogic.Configuration;
using MarketRateInfo.Server.BusinessLogic.ExternalRates.Providers;
using MarketRateInfo.Server.BusinessLogic.Interfaces;
using MarketRateInfo.Server.BusinessLogic.Services;
using MarketRateInfo.Server.DataAccess.Repositories;
using MarketRateInfo.Server.DataAccess.Entities;
using MarketRateInfo.Server.DataAccess;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace MarketRateInfo.Server;

/// <summary>
/// Provides extension methods for configuring application services and dependencies in the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application services, including identity, authorization, repositories, and external rate providers, to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail = true;

            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
        })
        .AddEntityFrameworkStores<MarketRateDbContext>()
        .AddSignInManager();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "MarketRateInfo.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/access-denied";

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

        services.AddAuthorization();
        services.AddScoped<IdentitySeedService>();

        services.AddScoped<IMarketRateRepository, MarketRateRepository>();
        services.AddScoped<IMarketRateService, MarketRateService>();
        services.AddScoped<IMarketRateRefreshService, MarketRateRefreshService>();
        services.AddScoped<IWeatherService, WeatherService>();

        services.AddSingleton<AkkaRuntime>();
        services.AddExternalRateProviders(configuration);

        services.AddHostedService<MarketRateBackgroundRefreshService>();

        return services;
    }

    /// <summary>
    /// Adds external market rate providers to the dependency injection container based on the configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    private static IServiceCollection AddExternalRateProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MarketRateProviderOptions>()
            .Bind(configuration.GetSection("MarketRates"))
            .Validate(options =>
                !options.Frankfurter.Enabled ||
                Uri.TryCreate(
                    options.Frankfurter.BaseUrl,
                    UriKind.Absolute,
                    out _),
                "MarketRates:Frankfurter:BaseUrl must be a valid absolute URI when enabled.")
            .Validate(options =>
                !options.Frankfurter.Enabled ||
                options.Frankfurter.TimeoutSeconds > 0,
                "MarketRates:Frankfurter:TimeoutSeconds must be greater than zero when enabled.")
            .Validate(options =>
                !options.ExchangeRateApi.Enabled ||
                Uri.TryCreate(
                    options.ExchangeRateApi.BaseUrl,
                    UriKind.Absolute,
                    out _),
                "MarketRates:ExchangeRateApi:BaseUrl must be a valid absolute URI when enabled.")
            .Validate(options =>
                !options.ExchangeRateApi.Enabled ||
                options.ExchangeRateApi.TimeoutSeconds > 0,
                "MarketRates:ExchangeRateApi:TimeoutSeconds must be greater than zero when enabled.")
            .Validate(options =>
                !options.CryptoRateApi.Enabled ||
                Uri.TryCreate(
                    options.CryptoRateApi.BaseUrl,
                    UriKind.Absolute,
                    out _),
                "MarketRates:CryptoRateApi:BaseUrl must be a valid absolute URI when enabled.")
            .Validate(options =>
                !options.CryptoRateApi.Enabled ||
                options.CryptoRateApi.TimeoutSeconds > 0,
                "MarketRates:CryptoRateApi:TimeoutSeconds must be greater than zero when enabled.")
            .ValidateOnStart();

        var providerOptions = configuration
            .GetSection("MarketRates")
            .Get<MarketRateProviderOptions>()
            ?? new MarketRateProviderOptions();

        // Registration order is the fallback order.

        if (providerOptions.Frankfurter.Enabled)
        {
            services.AddHttpClient<FrankfurterRateProvider>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<MarketRateProviderOptions>>()
                    .Value;

                client.BaseAddress = new Uri(
                    options.Frankfurter.BaseUrl,
                    UriKind.Absolute);

                client.Timeout = TimeSpan.FromSeconds(
                    options.Frankfurter.TimeoutSeconds);
            });

            services.AddTransient<IMarketRateProvider>(sp =>
                sp.GetRequiredService<FrankfurterRateProvider>());
        }

        if (providerOptions.ExchangeRateApi.Enabled)
        {
            services.AddHttpClient<ExchangeRateApiProvider>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<MarketRateProviderOptions>>()
                    .Value;

                client.BaseAddress = new Uri(
                    options.ExchangeRateApi.BaseUrl,
                    UriKind.Absolute);

                client.Timeout = TimeSpan.FromSeconds(
                    options.ExchangeRateApi.TimeoutSeconds);
            });

            services.AddTransient<IMarketRateProvider>(sp =>
                sp.GetRequiredService<ExchangeRateApiProvider>());
        }

        if (providerOptions.CryptoRateApi.Enabled)
        {
            services.AddHttpClient<CryptoRateApiProvider>((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<MarketRateProviderOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.CryptoRateApi.BaseUrl, UriKind.Absolute);

                client.Timeout = TimeSpan.FromSeconds(options.CryptoRateApi.TimeoutSeconds);

                client.DefaultRequestHeaders.UserAgent.ParseAdd("MarketRateInfo/1.0");

                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

                client.DefaultRequestVersion = HttpVersion.Version11;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
            });

            services.AddTransient<IMarketRateProvider>(sp =>
                sp.GetRequiredService<CryptoRateApiProvider>());
        }

        return services;
    }
}
