using AnimalFitness.Application.Abstractions;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Interceptors;
using AnimalFitness.Infrastructure.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.IntegrationTests.Persistence;

/// <summary>
/// Base de datos SQLite en memoria con el mismo modelo y las mismas reglas de escritura que SQL Server.
/// Sirve para probar el aislamiento sin depender de Docker.
/// </summary>
internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>Conexión abierta compartida por todos los contextos de esta base.</summary>
    public SqliteConnection Connection => _connection;

    /// <summary>
    /// Crea la estructura del modelo.
    /// </summary>
    public async Task EnsureCreatedAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Crea un contexto nuevo con la organización indicada. Sin organización, el filtro no devuelve filas de organización.
    /// </summary>
    /// <param name="tenantId">Organización activa, o nulo.</param>
    /// <param name="userId">Persona que actúa, o nulo.</param>
    public AppDbContext CreateContext(Guid? tenantId = null, Guid? userId = null)
    {
        var requestContext = new RequestContext();
        if (tenantId is { } tenant)
        {
            // En pruebas de persistencia el rol no se evalúa: basta con fijar la organización.
            requestContext.Establish(tenant, userId);
        }

        return CreateContext(requestContext);
    }

    private AppDbContext CreateContext(IRequestContext requestContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new TenantAuditInterceptor(requestContext, TimeProvider.System))
            .Options;

        return new AppDbContext(options, requestContext);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
