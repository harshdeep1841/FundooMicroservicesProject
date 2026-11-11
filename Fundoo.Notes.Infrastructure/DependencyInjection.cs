using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Application.Services;
using Fundoo.Notes.Infrastructure.Caching;
using Fundoo.Notes.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Fundoo.Notes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("NotesDb")
            ?? throw new InvalidOperationException(
                "Connection string 'NotesDb' is missing.");

        services.AddSingleton(
            _ => NpgsqlDataSource.Create(connectionString));

        services.AddScoped<INoteRepository, NoteRepository>();

        services.AddScoped<INoteService, NoteService>();

        var redisConnectionString =
            configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Redis connection string is missing.");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "Fundoo:";
        });

        services.AddScoped<INotesCache, RedisNotesCache>();

        return services;
    }
}