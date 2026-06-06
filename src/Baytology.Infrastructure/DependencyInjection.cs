using System.Text;
using Microsoft.Extensions.Http.Resilience;

using Baytology.Application.Common.Interfaces;
using Baytology.Infrastructure.Caching;
using Baytology.Infrastructure.Data;
using Baytology.Infrastructure.Identity;
using Baytology.Infrastructure.Interceptors;
using Baytology.Infrastructure.Notifications;
using Baytology.Infrastructure.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services
            .AddStartupInitializationServices(configuration)
            .AddCachingServices()
            .AddDatabaseServices(configuration, environment)
            .AddIdentityServices(configuration)
            .AddEmailServices(configuration, environment)
            .AddPaymentServices(configuration)
            .AddMessagingServices(configuration)
            .AddRealTimeServices()
            .AddAiFallbackServices(configuration)
            .AddExternalAiIntegrationServices(configuration, environment)
            .AddBackgroundJobs();

        return services;
    }

    private static IServiceCollection AddStartupInitializationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<StartupInitializationSettings>()
            .Bind(configuration.GetSection("StartupInitialization"));

        return services;
    }

    private static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024;
            options.MaximumKeyLength = 1024;
            options.DefaultEntryOptions = new()
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            };
        });

        services.AddScoped<IQueryCache, HybridQueryCache>();

        return services;
    }

    private static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DomainEventInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditLogInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required. Configure it via user-secrets or environment variables.");
        }

        var normalizedConnectionString = NormalizeConnectionString(connectionString, environment);

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(
                normalizedConnectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure();
                });
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }

    private static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("JwtSettings"))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Secret), "JwtSettings:Secret is required.")
            .Validate(settings => settings.Secret.Length >= 32, "JwtSettings:Secret must be at least 32 characters.")
            .ValidateOnStart();

        services.AddOptions<AdminSettings>()
            .Bind(configuration.GetSection("AdminSettings"));

        services.AddIdentity<AppUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
        {
            throw new InvalidOperationException("JwtSettings:Secret is required.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorizationBuilder();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddOptions<GoogleAuthSettings>()
            .Bind(configuration.GetSection("GoogleAuthSettings"))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ClientId), "GoogleAuthSettings:ClientId is required.")
            .ValidateOnStart();
        services.AddScoped<IExternalLoginTokenValidator, Baytology.Infrastructure.Identity.ExternalLoginTokenValidator>();

        return services;
    }

    private static IServiceCollection AddEmailServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var emailOptions = services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection("Email"))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.FromAddress), "Email:FromAddress is required.")
            .Validate(
                settings => settings.DeliveryMode != EmailDeliveryMode.Smtp
                    || !string.IsNullOrWhiteSpace(settings.SmtpHost),
                "Email:SmtpHost is required when DeliveryMode is Smtp.")
            .Validate(
                settings => settings.DeliveryMode != EmailDeliveryMode.Smtp || settings.SmtpPort > 0,
                "Email:SmtpPort must be greater than zero when DeliveryMode is Smtp.");

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            emailOptions.Validate(
                settings => settings.DeliveryMode == EmailDeliveryMode.Smtp,
                "Email:DeliveryMode must be Smtp outside Development.");
        }

        emailOptions.ValidateOnStart();

        services.AddScoped<IEmailSender, ConfiguredEmailSender>();

        return services;
    }

    private static string NormalizeConnectionString(string connectionString, IHostEnvironment environment)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        if (environment.IsDevelopment() && IsLocalSqlServer(builder.DataSource))
        {
            builder.DataSource = NormalizeLocalSqlServerDataSource(builder.DataSource);
            builder.Encrypt = false;
            builder.TrustServerCertificate = true;
        }

        return builder.ConnectionString;
    }

    private static bool IsLocalSqlServer(string? dataSource)
    {
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return false;
        }

        var normalized = dataSource.Trim();

        return normalized.Equals(".", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("(local)", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(@".\", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(@"(localdb)\", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(@"localhost\", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(@"127.0.0.1\", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLocalSqlServerDataSource(string? dataSource)
    {
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return "localhost";
        }

        var normalized = dataSource.Trim();

        if (normalized.Equals(".", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("(local)", StringComparison.OrdinalIgnoreCase))
        {
            return "localhost";
        }

        if (normalized.StartsWith(@".\", StringComparison.OrdinalIgnoreCase))
        {
            return $"localhost\\{normalized[2..]}";
        }

        return normalized;
    }

    private static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PaymobSettings>()
            .Bind(configuration.GetSection("Paymob"))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.ApiKey), "Paymob:ApiKey is required.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.IntegrationId.ToString()), "Paymob:IntegrationId is required.")
            .ValidateOnStart();

        services.AddScoped<IPaymentGateway, Baytology.Infrastructure.Payments.PaymobGateway>();
        services.AddHttpClient<Baytology.Infrastructure.Payments.PaymobGateway>();

        return services;
    }

    private static IServiceCollection AddMessagingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqSettings>()
            .Bind(configuration.GetSection("RabbitMQ"))
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.HostName), "RabbitMQ:HostName is required.")
            .ValidateOnStart();

        services.AddSingleton<IMessagePublisher, Baytology.Infrastructure.Messaging.RabbitMqPublisher>();

        return services;
    }

    private static IServiceCollection AddRealTimeServices(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<IConversationRealtimeService, Baytology.Infrastructure.RealTime.ConversationRealtimeService>();
        services.AddScoped<INotificationService, Baytology.Infrastructure.Notifications.NotificationService>();

        return services;
    }

    private static IServiceCollection AddAiFallbackServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiProcessingSettings>()
            .Bind(configuration.GetSection("AiProcessing"));

        services.AddScoped<Baytology.Application.Common.Interfaces.IAiDispatchPolicy, Baytology.Infrastructure.AI.RabbitMqAiDispatchPolicy>();
        services.AddScoped<Baytology.Application.Common.Interfaces.IAiSearchFallbackService, Baytology.Infrastructure.AI.InternalAiSearchFallbackService>();
        services.AddScoped<Baytology.Application.Common.Interfaces.IRecommendationFallbackService, Baytology.Infrastructure.AI.InternalRecommendationFallbackService>();

        return services;
    }

    private static IServiceCollection AddExternalAiIntegrationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<ExternalAiServicesSettings>()
            .Bind(configuration.GetSection("ExternalAiServices"));
        var externalAiTimeout = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue<int>("ExternalAiServices:TimeoutSeconds")));

        services.AddHttpClient<Baytology.Application.Common.Interfaces.IChatbotApiClient, Baytology.Infrastructure.AI.ChatbotApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAiServicesSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));

            if (Uri.TryCreate(settings.ChatbotBaseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
        })
        .ConfigurePrimaryHttpMessageHandler(sp => CreateExternalAiHttpHandler(
            environment,
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAiServicesSettings>>().Value))
        .AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = externalAiTimeout;
            options.TotalRequestTimeout.Timeout = externalAiTimeout;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, externalAiTimeout.TotalSeconds * 2));
        });

        services.AddHttpClient<Baytology.Application.Common.Interfaces.IRecommendationApiClient, Baytology.Infrastructure.AI.RecommendationApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAiServicesSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));

            if (Uri.TryCreate(settings.RecommendationBaseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
        })
        .ConfigurePrimaryHttpMessageHandler(sp => CreateExternalAiHttpHandler(
            environment,
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAiServicesSettings>>().Value))
        .AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = externalAiTimeout;
            options.TotalRequestTimeout.Timeout = externalAiTimeout;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, externalAiTimeout.TotalSeconds * 2));
        });

        services.AddHttpClient<Baytology.Application.Common.Interfaces.IVoiceRecognitionApiClient, Baytology.Infrastructure.AI.VoiceRecognitionApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAiServicesSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));

            var baseUrl = string.IsNullOrWhiteSpace(settings.VoiceRecognitionBaseUrl)
                ? settings.ChatbotBaseUrl
                : settings.VoiceRecognitionBaseUrl;

            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
        })
        .ConfigurePrimaryHttpMessageHandler(sp => CreateExternalAiHttpHandler(
            environment,
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAiServicesSettings>>().Value))
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.ShouldHandle = static _ => ValueTask.FromResult(false);
            options.AttemptTimeout.Timeout = externalAiTimeout;
            options.TotalRequestTimeout.Timeout = externalAiTimeout;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, externalAiTimeout.TotalSeconds * 2));
        });

        services.AddHttpClient<Baytology.Application.Common.Interfaces.IImageSearchApiClient, Baytology.Infrastructure.AI.ImageSearchApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAiServicesSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds));

            var baseUrl = string.IsNullOrWhiteSpace(settings.ImageSearchBaseUrl)
                ? settings.ChatbotBaseUrl
                : settings.ImageSearchBaseUrl;

            if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                client.BaseAddress = baseUri;
        })
        .ConfigurePrimaryHttpMessageHandler(sp => CreateExternalAiHttpHandler(
            environment,
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ExternalAiServicesSettings>>().Value))
        .AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = externalAiTimeout;
            options.TotalRequestTimeout.Timeout = externalAiTimeout;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, externalAiTimeout.TotalSeconds * 2));
        });

        return services;
    }

    private static System.Net.Http.HttpClientHandler CreateExternalAiHttpHandler(
        IHostEnvironment environment,
        ExternalAiServicesSettings settings)
    {
        var handler = new System.Net.Http.HttpClientHandler();

        if (environment.IsDevelopment() && settings.AllowUntrustedCertificates)
        {
            handler.ServerCertificateCustomValidationCallback =
                System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    }

    private static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddHostedService<Baytology.Infrastructure.BackgroundJobs.OutboxProcessor>();
        services.AddHostedService<Baytology.Infrastructure.BackgroundJobs.AiFallbackRecoveryProcessor>();
        return services;
    }
}
