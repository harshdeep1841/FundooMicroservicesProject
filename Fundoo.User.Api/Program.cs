using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Fundoo.User.Api.Validation;
using Fundoo.User.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddUserInfrastructure(builder.Configuration);

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fundoo.User.Api",
        Version = "v1",
        Description = "User registration and authentication API"
    });

    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Fundoo.User.Api",
        Version = "v2",
        Description = "Fundoo User API Version 2"
    });

    options.DocInclusionPredicate((documentName, apiDescription) =>
    {
        return apiDescription.GroupName == documentName;
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("UserApiPolicy", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromSeconds(30);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Fundoo.User.Api v1");

        options.SwaggerEndpoint(
            "/swagger/v2/swagger.json",
            "Fundoo.User.Api v2");
    });
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapGet("/test", () => "Hello World!");

app.MapControllers();

app.Run();
