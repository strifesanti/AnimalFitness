using AnimalFitness.Application.Abstractions;
using AnimalFitness.Application.Abstractions.Email;
using AnimalFitness.Application.Abstractions.Tenancy;
using AnimalFitness.Application.Features.Auth;
using AnimalFitness.Application.Features.Attendance;
using AnimalFitness.Application.Features.Challenges;
using AnimalFitness.Application.Features.Communication;
using AnimalFitness.Application.Features.Nutrition;
using AnimalFitness.Application.Features.Organizations;
using AnimalFitness.Application.Features.Profiles;
using AnimalFitness.Application.Features.Progress;
using AnimalFitness.Application.Features.Training;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Advanced;
using AnimalFitness.Infrastructure.Auth;
using AnimalFitness.Infrastructure.Email;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Identity;
using AnimalFitness.Infrastructure.Persistence.Interceptors;
using AnimalFitness.Infrastructure.Progress;
using AnimalFitness.Infrastructure.Tenancy;
using AnimalFitness.Infrastructure.Training;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalFitness.Infrastructure;

/// <summary>
/// Registro de servicios de infraestructura: base de datos, Identity y contexto multi-tenant.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Nombre de la cadena de conexión principal. Se configura con variables de entorno o user-secrets, nunca en el repositorio.
    /// </summary>
    public const string ConnectionStringName = "AnimalFitness";

    /// <summary>
    /// Clave de configuración del proveedor. Valores: <c>SqlServer</c> (predeterminado) o <c>Sqlite</c> (pruebas).
    /// </summary>
    public const string DatabaseProviderKey = "Database:Provider";

    /// <summary>
    /// Agrega la persistencia, Identity, correo provisional y el contexto de la petición.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <param name="configuration">Configuración con la cadena de conexión y el proveedor.</param>
    /// <returns>La misma colección, para encadenar registros.</returns>
    /// <exception cref="InvalidOperationException">Si falta la cadena de conexión para SQL Server.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<RequestContext>();
        services.AddScoped<IRequestContext>(provider => provider.GetRequiredService<RequestContext>());
        services.AddScoped<ITenantAccessService, TenantAccessService>();
        services.AddScoped<IPlatformQueryService, PlatformQueryService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<AdminAccess>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IMembershipAdminService, MembershipAdminService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IJoinRequestService, JoinRequestService>();
        services.AddScoped<IProfileAdminService, ProfileAdminService>();
        services.AddScoped<IExerciseService, ExerciseService>();
        services.AddScoped<IWorkoutService, WorkoutService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<AthleteResolver>();
        services.AddScoped<INutritionService, NutritionService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IChallengeService, ChallengeService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<TenantAuditInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            ConfigureDatabase(options, configuration);
            options.AddInterceptors(provider.GetRequiredService<TenantAuditInterceptor>());
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }

    private static void ConfigureDatabase(DbContextOptionsBuilder options, IConfiguration configuration)
    {
        var provider = configuration[DatabaseProviderKey] ?? "SqlServer";
        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var sqliteConnection = configuration.GetConnectionString(ConnectionStringName)
                ?? "DataSource=:memory:";
            options.UseSqlite(sqliteConnection);
            return;
        }

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Falta la cadena de conexión 'ConnectionStrings:{ConnectionStringName}'. " +
                "Configúrala con la variable de entorno ConnectionStrings__AnimalFitness o con user-secrets.");

        options.UseSqlServer(connectionString);
    }
}
