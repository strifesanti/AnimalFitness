using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Identity;
using AnimalFitness.Infrastructure.Persistence.Interceptors;
using AnimalFitness.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalFitness.IntegrationTests.Tenancy;

/// <summary>
/// Host de prueba con SQLite en memoria, autenticación de prueba y datos de tenancy ya sembrados.
/// </summary>
public sealed class TenancyApiFactory : WebApplicationFactory<Program>
{
    private static readonly DateTimeOffset SeedTime = new(2026, 11, 13, 15, 0, 0, TimeSpan.Zero);

    private readonly SqliteTestDatabase _database = new();
    private readonly object _seedLock = new();
    private bool _seeded;

    /// <summary>Organización activa de demostración.</summary>
    public Guid ActiveTenantId { get; private set; }

    /// <summary>Organización suspendida de demostración.</summary>
    public Guid SuspendedTenantId { get; private set; }

    /// <summary>Persona con membresía Trainer en la organización activa.</summary>
    public Guid MemberId { get; private set; }

    /// <summary>Persona sin membresía en la organización activa.</summary>
    public Guid OutsiderId { get; private set; }

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
            // Sustituye la conexión SQLite por la instancia compartida y abierta.
            // Ambos registros usan SQLite, así que no hay conflicto de proveedores.
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

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            var database = _database;
            services.AddDbContext<AppDbContext>((provider, options) => options
                .UseSqlite(database.Connection)
                .AddInterceptors(provider.GetRequiredService<TenantAuditInterceptor>()));
        });
    }

    /// <summary>
    /// Crea el esquema y los datos de demostración una sola vez.
    /// </summary>
    public void EnsureSeeded()
    {
        lock (_seedLock)
        {
            if (_seeded)
            {
                return;
            }

            _database.EnsureCreatedAsync().GetAwaiter().GetResult();

            var active = Tenant.Create("Academia Norte", "academia-norte", "norte@animalfitness.test", SeedTime);
            active.Activate(SeedTime.AddMinutes(1), null);
            var suspended = Tenant.Create("Academia Sur", "academia-sur", "sur@animalfitness.test", SeedTime);
            suspended.Suspend(SeedTime.AddMinutes(1), null);

            ActiveTenantId = active.Id;
            SuspendedTenantId = suspended.Id;
            MemberId = Guid.CreateVersion7();
            OutsiderId = Guid.CreateVersion7();

            using var db = _database.CreateContext();
            db.Tenants.AddRange(active, suspended);
            db.Users.AddRange(
                new ApplicationUser(MemberId) { UserName = "miembro@animalfitness.test", Email = "miembro@animalfitness.test" },
                new ApplicationUser(OutsiderId) { UserName = "otro@animalfitness.test", Email = "otro@animalfitness.test" });
            db.UserProfiles.AddRange(
                User.Create("miembro@animalfitness.test", "Mia", "Miembro", SeedTime, id: MemberId),
                User.Create("otro@animalfitness.test", "Otto", "Otro", SeedTime, id: OutsiderId));
            db.TenantMemberships.Add(TenantMembership.Create(ActiveTenantId, MemberId, MembershipRole.Trainer, SeedTime, null));
            db.TenantMemberships.Add(TenantMembership.Create(SuspendedTenantId, MemberId, MembershipRole.Athlete, SeedTime, null));
            db.SaveChanges();
            _seeded = true;
        }
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
