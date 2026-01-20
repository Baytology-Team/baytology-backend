using System.Text;

using Baytology.Application.Common.Interfaces;
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
            .AddDatabaseServices(configuration, environment)
            .AddIdentityServices(configuration)
            .AddEmailServices(configuration, environment);

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
}
