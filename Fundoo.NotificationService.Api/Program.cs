
using Confluent.Kafka;
using Fundoo.NotificationService.Api.Middleware;
using Fundoo.NotificationService.Application.Events;
using Fundoo.NotificationService.Application.Interfaces;
using Fundoo.NotificationService.Infrastructure.Email;
using Fundoo.NotificationService.Infrastructure.Messaging.Kafka;
using Fundoo.Observability;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
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

// Existing email sender
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Kafka configuration
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));

// Validate Kafka configuration
var kafkaOptions = builder.Configuration
    .GetSection("Kafka")
    .Get<KafkaOptions>()
    ?? throw new InvalidOperationException(
        "Kafka configuration is missing.");

if (string.IsNullOrWhiteSpace(kafkaOptions.BootstrapServers))
    throw new InvalidOperationException(
        "Kafka BootstrapServers is missing.");

if (string.IsNullOrWhiteSpace(kafkaOptions.PasswordResetTopic))
    throw new InvalidOperationException(
        "Kafka PasswordResetTopic is missing.");

if (string.IsNullOrWhiteSpace(kafkaOptions.GroupId))
    throw new InvalidOperationException(
        "Kafka GroupId is missing.");

// Kafka consumer message processor
builder.Services.AddScoped<PasswordResetEventConsumer>();

// Kafka background consumer
builder.Services.AddHostedService<KafkaConsumerBackgroundService>();

// Existing services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddFundooCorrelationIds();

var app = builder.Build();

// Existing middleware
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ServiceKeyMiddleware>();

app.MapControllers();

app.Run();
