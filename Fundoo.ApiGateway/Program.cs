using System.Threading.RateLimiting;
using MMLib.SwaggerForOcelot.DependencyInjection;
using MMLib.SwaggerForOcelot.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Select configuration according to the hosting environment.
var ocelotConfigFile = builder.Environment.IsDevelopment()
    ? "ocelot.Development.json"
    : "ocelot.json";

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile(
        ocelotConfigFile,
        optional: false,
        reloadOnChange: true);

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"Ocelot configuration loaded: {ocelotConfigFile}");

builder.Services.AddOcelot(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey:
                    context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",

                    factory => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        AutoReplenishment = true,
                        QueueLimit = 0
                    }));

    options.RejectionStatusCode = 429;
});

builder.Services.AddSwaggerForOcelot(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwaggerForOcelotUI(options =>
{
    options.PathToSwaggerGenerator = "/swagger/docs";
});

app.UseRateLimiter();

await app.UseOcelot();

await app.RunAsync();
