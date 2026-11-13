using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace AnimalFitness.IntegrationTests.Persistence;

/// <summary>
/// Aplica las migraciones sobre SQL Server real. Requiere Docker en ejecución.
/// Ejecución: dotnet test --filter "Category=SqlServer"
/// </summary>
[Trait("Category", "SqlServer")]
public sealed class SqlServerMigrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Migrations_apply_cleanly_on_sql_server()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        await using var context = new AppDbContext(options, new RequestContext());

        await context.Database.MigrateAsync();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(await context.Tenants.AnyAsync());
    }
}
