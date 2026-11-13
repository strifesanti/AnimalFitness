using AnimalFitness.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AnimalFitness.Infrastructure.Persistence;

/// <summary>
/// Crea el contexto solo para las herramientas de EF Core (migraciones y scripts). No abre ninguna conexión real.
/// La cadena es de diseño; las migraciones se generan a partir del modelo, no de la base de datos.
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc />
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=AnimalFitness_Design;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options, new RequestContext());
    }
}
