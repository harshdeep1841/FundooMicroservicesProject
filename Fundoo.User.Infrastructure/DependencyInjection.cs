
using System.Text;
using Fundoo.User.Application.Interfaces;
using Fundoo.User.Application.Services;
using Fundoo.User.Infrastructure.Persistence;
using Fundoo.User.Infrastructure.Repositories;
using Fundoo.User.Infrastructure.Security;
using Fundoo.User.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        services.Configure<JwtOptions>(
            configuration.GetSection("Jwt"));

        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddScoped<IAuthService, AuthService>();

        var jwt = configuration.GetSection("Jwt");

        var signingKey = configuration["Jwt:SigningKey"];

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                        configuration["NotificationService:BaseUrl"]!);

                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout =
                    TimeSpan.FromSeconds(8);

                options.CircuitBreaker.FailureRatio = 1.0;

                options.CircuitBreaker.MinimumThroughput = 3;

                options.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromSeconds(30);

                options.CircuitBreaker.BreakDuration =
                    TimeSpan.FromSeconds(15);
                options.Retry.MaxRetryAttempts = 3; //default retries attempt = 2
                options.Retry.DisableForUnsafeHttpMethods();
            });
    }
}

