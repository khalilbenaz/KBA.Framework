using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KBA.Framework.Domain.Common;

/// <summary>
/// Extensions pour configurer les services avec support de plusieurs chaînes de connexion
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute le support de plusieurs chaînes de connexion
    /// </summary>
    public static IServiceCollection AddMultipleConnectionStrings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionStringsOptions = new ConnectionStringsOptions();
        configuration.GetSection(ConnectionStringsOptions.SectionName).Bind(connectionStringsOptions);
        
        services.AddSingleton(connectionStringsOptions);
        services.AddSingleton<IConnectionStringProvider>(
            sp => new DefaultConnectionStringProvider(connectionStringsOptions));

        return services;
    }

    /// <summary>
    /// Ajoute un DbContext avec support de plusieurs chaînes de connexion
    /// </summary>
    public static IServiceCollection AddDbContextWithMultipleConnections<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string migrationsAssembly,
        DatabaseOperationType defaultOperationType = DatabaseOperationType.Write)
        where TContext : DbContext
    {
        services.AddMultipleConnectionStrings(configuration);

        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            var connectionStringProvider = serviceProvider.GetRequiredService<IConnectionStringProvider>();
            var connectionString = connectionStringProvider.GetConnectionString(defaultOperationType);

            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(migrationsAssembly));
        });

        return services;
    }

    /// <summary>
    /// Ajoute une factory pour créer des DbContext avec différentes chaînes de connexion
    /// </summary>
    public static IServiceCollection AddDbContextFactory<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string migrationsAssembly)
        where TContext : DbContext
    {
        services.AddMultipleConnectionStrings(configuration);

        services.AddTransient<Func<DatabaseOperationType, TContext>>(serviceProvider =>
        {
            return (operationType) =>
            {
                var connectionStringProvider = serviceProvider.GetRequiredService<IConnectionStringProvider>();
                var connectionString = connectionStringProvider.GetConnectionString(operationType);

                var optionsBuilder = new DbContextOptionsBuilder<TContext>();
                optionsBuilder.UseSqlServer(connectionString, b => b.MigrationsAssembly(migrationsAssembly));

                return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
            };
        });

        return services;
    }
}
