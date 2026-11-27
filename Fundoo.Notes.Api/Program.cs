using System.Text;
using System.Security.Claims;
using Fundoo.Notes.Infrastructure;
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Observability;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] " +
            "[CorrelationId: {CorrelationId}] " +
            "{Message:lj}{NewLine}{Exception}");
});

builder.Services.AddNotesInfrastructure(builder.Configuration);

var connectionString =
    builder.Configuration.GetConnectionString("NotesDb")
    ?? throw new InvalidOperationException(
        "Connection string 'NotesDb' is missing.");

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: connectionString,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" })
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" });

var signingKey =
    builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key is missing.");

var issuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is missing.");

var audience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(signingKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var userIdValue = principal?.FindFirst("sub")?.Value
                    ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionIdValue = principal?.FindFirst("sid")?.Value
                    ?? principal?.FindFirst(ClaimTypes.Sid)?.Value;

                if (!Guid.TryParse(userIdValue, out var userId) ||
                    !Guid.TryParse(sessionIdValue, out var sessionId))
                {
                    context.Fail("The access token has no valid session.");
                    return;
                }

                try
                {
                    var userDirectory = context.HttpContext.RequestServices
                        .GetRequiredService<IUserDirectoryClient>();
                    var active = await userDirectory.IsSessionActiveAsync(
                        userId, sessionId, context.HttpContext.RequestAborted);

                    if (!active)
                        context.Fail("The session has expired or has been ended.");
                }
                catch (HttpRequestException exception)
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("SessionValidation");
                    logger.LogError(exception, "User service session validation failed.");
                    context.Fail("Session validation is temporarily unavailable.");
                }
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");

                logger.LogError(
                    context.Exception,
                    "JWT authentication failed.");

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Your sign-in session is no longer valid. Refresh it or sign in again."
                });
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fundoo Notes API",
        Version = "v1",
        Description = "Notes and labels management API"
    });

    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token."
        });

    options.AddSecurityDefinition("ClientId",
        new OpenApiSecurityScheme
        {
            Name = "ClientId",
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "Enter your Ocelot Client ID. Example: test-client-1"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            },
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ClientId"
                    }
                },
                Array.Empty<string>()
            }
        });
});

builder.Services.AddFundooCorrelationIds();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
