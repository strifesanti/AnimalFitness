using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AnimalFitness.IntegrationTests;

public sealed class AnimalFitnessApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Cadena de conexión de prueba. No se abre conexión en las pruebas que solo usan HTTP.
    /// </summary>
    internal const string TestConnectionString = "Server=localhost;Database=AnimalFitness_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Las pruebas de humo no tocan la base de datos; SQLite evita exigir SQL Server.
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:AnimalFitness", "DataSource=:memory:");
    }
}
