using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Domain.Training;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnimalFitness.Infrastructure.Seeding;

/// <summary>
/// Carga datos de demostración y el SuperAdmin de desarrollo.
/// Solo corre en Development. Las credenciales del SuperAdmin salen de configuración/user-secrets, nunca del código.
/// </summary>
public static class DevelopmentDataSeeder
{
    /// <summary>
    /// Aplica migraciones (o EnsureCreated en SQLite) y siembra datos de Development.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentDataSeeder");
        var configuration = services.GetRequiredService<IConfiguration>();
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (db.Database.IsSqlServer())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            // SQLite de Development usa EnsureCreated (las migraciones son para SQL Server).
            // EnsureCreated no actualiza un .db ya creado: si falta una tabla de fases nuevas, recreamos.
            await EnsureSqliteSchemaAsync(db, logger, cancellationToken);
        }

        if (!await roleManager.RoleExistsAsync(PlatformRoles.SuperAdmin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(PlatformRoles.SuperAdmin));
        }

        await EnsureSuperAdminAsync(configuration, userManager, db, logger, cancellationToken);
        await EnsureDemoTenantsAsync(db, userManager, logger, cancellationToken);
    }

    private static async Task EnsureSqliteSchemaAsync(
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var recreate = false;
        if (await db.Database.CanConnectAsync(cancellationToken))
        {
            try
            {
                // Tabla de Phase 2: si falta, el .db quedó congelado por EnsureCreated.
                _ = await db.AthleteProfiles.AsNoTracking().Take(1).CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                recreate = true;
                logger.LogWarning(
                    ex,
                    "SQLite de desarrollo desactualizado. Se recrea el esquema completo.");
            }
        }

        if (recreate)
        {
            await db.Database.EnsureDeletedAsync(cancellationToken);
        }

        await db.Database.EnsureCreatedAsync(cancellationToken);
    }

    private static async Task EnsureSuperAdminAsync(
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var email = configuration["Seed:SuperAdmin:Email"];
        var password = configuration["Seed:SuperAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "SuperAdmin de desarrollo omitido: configura Seed:SuperAdmin:Email y Seed:SuperAdmin:Password con user-secrets.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, PlatformRoles.SuperAdmin))
            {
                await userManager.AddToRoleAsync(existing, PlatformRoles.SuperAdmin);
            }

            return;
        }

        var userId = Guid.CreateVersion7();
        var account = new ApplicationUser(userId)
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        var create = await userManager.CreateAsync(account, password);
        if (!create.Succeeded)
        {
            logger.LogError(
                "No se pudo crear el SuperAdmin: {Errors}",
                string.Join("; ", create.Errors.Select(error => error.Description)));
            return;
        }

        await userManager.AddToRoleAsync(account, PlatformRoles.SuperAdmin);

        var now = DateTimeOffset.UtcNow;
        var profile = User.Create(
            email,
            configuration["Seed:SuperAdmin:FirstName"] ?? "Super",
            configuration["Seed:SuperAdmin:LastName"] ?? "Admin",
            now,
            id: userId);
        profile.ConfirmEmail(now, userId);
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("SuperAdmin de desarrollo creado: {Email}", email);
    }

    private static async Task EnsureDemoTenantsAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var tenantA = Tenant.Create("Academia Demo Norte", "academia-norte", "norte@demo.animalfitness.test", now);
        tenantA.Activate(now.AddMinutes(1), null);
        var tenantB = Tenant.Create("Academia Demo Sur", "academia-sur", "sur@demo.animalfitness.test", now);
        tenantB.Activate(now.AddMinutes(1), null);
        db.Tenants.AddRange(tenantA, tenantB);
        db.OrganizationSettings.Add(TenantSettings.Create(tenantA.Id, now, null));
        db.OrganizationSettings.Add(TenantSettings.Create(tenantB.Id, now, null));

        var owner = await CreateDemoUserAsync(userManager, db, "owner@demo.animalfitness.test", "Olivia", "Owner", "OwnerDemo1!", now);
        var trainer = await CreateDemoUserAsync(userManager, db, "trainer@demo.animalfitness.test", "Tomas", "Trainer", "TrainerDemo1!", now);
        var athlete = await CreateDemoUserAsync(userManager, db, "athlete@demo.animalfitness.test", "Ana", "Athlete", "AthleteDemo1!", now);

        db.TenantMemberships.Add(TenantMembership.Create(tenantA.Id, owner.Id, MembershipRole.Owner, now, null));
        db.TenantMemberships.Add(TenantMembership.Create(tenantA.Id, trainer.Id, MembershipRole.Trainer, now, null));
        db.TenantMemberships.Add(TenantMembership.Create(tenantA.Id, athlete.Id, MembershipRole.Athlete, now, null));
        db.TenantMemberships.Add(TenantMembership.Create(tenantB.Id, athlete.Id, MembershipRole.Athlete, now, null));

        var trainerProfile = TrainerProfile.Create(tenantA.Id, trainer.Id, now, null, "Entrenador demo", "Fuerza, hipertrofia");
        var athleteProfileA = AthleteProfile.Create(tenantA.Id, athlete.Id, now, null, 170, 70);
        var athleteProfileB = AthleteProfile.Create(tenantB.Id, athlete.Id, now, null, 170, 70);
        db.TrainerProfiles.Add(trainerProfile);
        db.AthleteProfiles.AddRange(athleteProfileA, athleteProfileB);
        db.TrainerAthletes.Add(TrainerAthlete.Create(tenantA.Id, trainerProfile.Id, athleteProfileA.Id, now, null));
        db.AthleteGoals.Add(AthleteGoal.Create(
            tenantA.Id,
            athleteProfileA.Id,
            "Bajar grasa corporal",
            now,
            null,
            "Meta de demostración",
            15,
            "%"));

        SeedDemoTraining(db, tenantA.Id, trainerProfile.Id, athleteProfileA.Id, trainer.Id, now);

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Datos demo de Development creados (academias norte/sur y usuarios owner/trainer/athlete).");
    }

    private static void SeedDemoTraining(
        AppDbContext db,
        Guid tenantId,
        Guid trainerProfileId,
        Guid athleteProfileId,
        Guid trainerUserId,
        DateTimeOffset now)
    {
        var category = ExerciseCategory.Create(tenantId, "Fuerza", now, trainerUserId, "Ejercicios de fuerza", 1);
        db.ExerciseCategories.Add(category);

        var squat = Exercise.Create(
            tenantId, "Sentadilla", "Piernas", ExerciseType.Gym, ExerciseDifficulty.Intermediate,
            now, trainerUserId, category.Id, "Sentadilla con barra", "Glúteos", "Barra");
        var bench = Exercise.Create(
            tenantId, "Press de banca", "Pecho", ExerciseType.Gym, ExerciseDifficulty.Intermediate,
            now, trainerUserId, category.Id, "Press horizontal con barra", "Tríceps", "Barra y banco");
        var pushUp = Exercise.Create(
            tenantId, "Flexiones", "Pecho", ExerciseType.Calisthenics, ExerciseDifficulty.Beginner,
            now, trainerUserId, category.Id, "Flexiones de brazos", "Tríceps");
        db.Exercises.AddRange(squat, bench, pushUp);

        var plan = WorkoutPlan.Create(
            tenantId, "Fuerza base", now, trainerUserId, "Plan de demostración", "Fuerza", trainerProfileId);
        var day = WorkoutDay.Create(tenantId, plan.Id, 1, "Día A", now, trainerUserId, "Tren completo");
        db.WorkoutPlans.Add(plan);
        db.WorkoutDays.Add(day);
        db.WorkoutExercises.AddRange(
            WorkoutExercise.Create(tenantId, day.Id, squat.Id, 1, 4, now, trainerUserId, "6-8", restSeconds: 120),
            WorkoutExercise.Create(tenantId, day.Id, bench.Id, 2, 4, now, trainerUserId, "6-8", restSeconds: 120),
            WorkoutExercise.Create(tenantId, day.Id, pushUp.Id, 3, 3, now, trainerUserId, "10-15", restSeconds: 60));

        var publishedAt = now.AddMinutes(1);
        plan.Publish(publishedAt, trainerUserId);
        db.WorkoutPlanAssignments.Add(
            WorkoutPlanAssignment.Create(tenantId, plan.Id, athleteProfileId, publishedAt, trainerUserId));
    }

    private static async Task<ApplicationUser> CreateDemoUserAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        string email,
        string firstName,
        string lastName,
        string password,
        DateTimeOffset now)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var id = Guid.CreateVersion7();
        var account = new ApplicationUser(id)
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };
        var result = await userManager.CreateAsync(account, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo crear el usuario demo {email}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }

        var profile = User.Create(email, firstName, lastName, now, id: id);
        profile.ConfirmEmail(now, id);
        db.UserProfiles.Add(profile);
        return account;
    }
}
