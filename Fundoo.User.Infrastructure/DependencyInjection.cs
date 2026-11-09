
using System.Text;
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Application.Services;
using Fundoo.User.Infrastructure.Persistence;
using Fundoo.User.Infrastructure.Repositories;
using Fundoo.User.Infrastructure.Security;
using Fundoo.User.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;

namespace Fundoo.User.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUserInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddRepositories(services);
        AddSecurity(services, configuration);
        AddNotificationClient(services, configuration);

        return services;
    }

    private static void AddDatabase(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("UserDb")
            ?? throw new InvalidOperationException(
                "Connection string 'UserDb' was not found.");

        services.AddDbContext<UserDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
    }

    private static void AddSecurity(
        IServiceCollection services,
        IConfiguration configuration)
    {
        // Password hashing
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        // JWT configuration
        services.Configure<JwtOptions>(
            configuration.GetSection("Jwt"));

        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddScoped<IAuthService, AuthService>();

        var jwt = configuration.GetSection("Jwt");

        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "JWT SigningKey is missing.");

        var googleClientId =
            configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException(
                "Google ClientId is missing.");

        var googleClientSecret =
            configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException(
                "Google ClientSecret is missing.");
        
        
        services.Configure<ServiceAuthOptions>(
            configuration.GetSection("ServiceAuth"));
        services.AddScoped<ServiceTokenService>();
        
        
        // Authentication schemes
        services
            .AddAuthentication(options =>
            {
                // Existing Fundoo APIs use JWT authentication
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })

            // Fundoo JWT authentication
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt["Issuer"],

                        ValidateAudience = true,
                        ValidAudience = jwt["Audience"],

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(signingKey)),

                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
            })

            // Temporary cookie for Google OIDC authentication
            .AddCookie("GoogleCookie", options =>
            {
                options.Cookie.Name = "Fundoo.Google.Session";

                options.Cookie.HttpOnly = true;

                options.Cookie.SecurePolicy =
                    CookieSecurePolicy.Always;

                options.ExpireTimeSpan =
                    TimeSpan.FromMinutes(20);

                options.SlidingExpiration = false;
            })

            // Google OpenID Connect
            .AddOpenIdConnect("Google", options =>
            {
                options.Authority =
                    "https://accounts.google.com";

                options.ClientId = googleClientId;

                options.ClientSecret = googleClientSecret;

                options.CallbackPath = "/signin-oidc";

                options.SignInScheme = "GoogleCookie";

                options.ResponseType = "code";

                options.UsePkce = true;

                options.Scope.Clear();

                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.SaveTokens = false;
            });

        services.AddAuthorization();
    }

    private static void AddNotificationClient(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddHttpClient<INotificationClient, NotificationClient>(
                client =>
                {
                    client.BaseAddress = new Uri(
                        configuration["NotificationService:BaseUrl"]
                        ?? throw new InvalidOperationException(
                            "NotificationService:BaseUrl is missing."));

                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
            .AddStandardResilienceHandler(options =>
            {
                // Concurrency limiter (bulkhead)
                options.RateLimiter.DefaultRateLimiterOptions.PermitLimit = 10;

                options.RateLimiter.DefaultRateLimiterOptions.QueueLimit = 5;

                // Timeout per attempt
                options.AttemptTimeout.Timeout =
                    TimeSpan.FromSeconds(8);

                // Circuit breaker
                options.CircuitBreaker.FailureRatio = 1.0;

                options.CircuitBreaker.MinimumThroughput = 3;

                options.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(30);

                options.CircuitBreaker.BreakDuration =
                    TimeSpan.FromSeconds(15);

                // Retry
                options.Retry.MaxRetryAttempts = 3;

                options.Retry.DisableForUnsafeHttpMethods();
            });
    }
}
