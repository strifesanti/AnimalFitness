using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Interceptors;
using AnimalFitness.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalFitness.IntegrationTests.Auth;

/// <summary>
/// Host de prueba con cookies Identity reales y SQLite compartida.
/// </summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteTestDatabase _database = new();
    private bool _schemaReady;

    /// <summary>Base de datos compartida.</summary>
    internal SqliteTestDatabase Database => _database;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:AnimalFitness", "DataSource=:memory:");

        builder.ConfigureTestServices(services =>
        {
            var toRemove = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(AppDbContext)
                    || descriptor.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || (descriptor.ServiceType.IsGenericType
                        && descriptor.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal)
                        && descriptor.ServiceType.GenericTypeArguments.FirstOrDefault() == typeof(AppDbContext)))
                .ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            var database = _database;
            services.AddDbContext<AppDbContext>((provider, options) => options
                .UseSqlite(database.Connection)
                .AddInterceptors(provider.GetRequiredService<TenantAuditInterceptor>()));
        });
    }

    /// <summary>Crea el esquema una sola vez.</summary>
    public void EnsureSchema()
    {
        if (_schemaReady)
        {
            return;
        }

        _database.EnsureCreatedAsync().GetAwaiter().GetResult();
        _schemaReady = true;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _database.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }
}
