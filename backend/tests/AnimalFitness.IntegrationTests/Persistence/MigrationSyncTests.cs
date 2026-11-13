using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.IntegrationTests.Persistence;

public sealed class MigrationSyncTests
{
    [Fact]
    public void Migrations_match_the_current_model()
    {
        // No abre ninguna conexión: compara el modelo actual con la última instantánea de migraciones.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=AnimalFitness_Sync;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var context = new AppDbContext(options, new RequestContext());

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "El modelo cambió sin una migración nueva. Ejecuta dotnet ef migrations add.");
    }
}
