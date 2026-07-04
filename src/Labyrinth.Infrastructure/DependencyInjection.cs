using Labyrinth.Application.Abstractions;
using Labyrinth.Application.Services;
using Labyrinth.Domain;
using Labyrinth.Infrastructure.Content;
using Labyrinth.Infrastructure.Persistence;
using Labyrinth.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Labyrinth.Infrastructure;

/// <summary>One place to register every concrete adapter behind the Application ports.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddLabyrinthInfrastructure(
        this IServiceCollection services,
        string sqliteConnectionString,
        string gameDataPath,
        JwtOptions jwtOptions)
    {
        // Persistence
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(sqliteConnectionString));
        services.AddScoped<IPlayerAccountStore, EfPlayerAccountStore>();

        // Static content (immutable → singleton)
        services.AddSingleton<ISectionRepository>(_ => new JsonSectionRepository(gameDataPath));

        // Security & randomness
        services.AddSingleton(jwtOptions);
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IDiceRoller, SystemDiceRoller>();

        // Domain services
        services.AddSingleton<CombatResolver>();
        services.AddSingleton<CharacterFactory>();

        // Application services
        services.AddScoped<GameStateFactory>();
        services.AddScoped<CombatService>();
        services.AddScoped<AuthService>();
        services.AddScoped<GameService>();

        return services;
    }
}
