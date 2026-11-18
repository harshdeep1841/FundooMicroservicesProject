using Fundoo.NotificationService.Api.Middleware;
using Fundoo.NotificationService.Application.Interfaces;
using Fundoo.NotificationService.Infrastructure.Email;
using Fundoo.Observability;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
//COIDS
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
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddFundooCorrelationIds();
var app = builder.Build();
//COIDS
app.UseMiddleware<CorrelationIdMiddleware>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ServiceKeyMiddleware>();
app.MapControllers();
app.Run();
