using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Attendance;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Domain.Tracking;
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
        await EnsureHistoricalDemoRosterAsync(db, userManager, logger, cancellationToken);
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

    /// <summary>
    /// Usuarios y actividad de los últimos ~6 meses. Idempotente: si ya existe alumno01, no repite.
    /// Funciona aunque las academias base ya existieran (p. ej. redeploy en Railway).
    /// </summary>
    private static async Task EnsureHistoricalDemoRosterAsync(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        const string markerEmail = "alumno01@demo.animalfitness.test";
        if (await userManager.FindByEmailAsync(markerEmail) is not null)
        {
            return;
        }

        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Slug == "academia-norte", cancellationToken);
        if (tenant is null)
        {
            logger.LogWarning("Roster histórico omitido: no existe academia-norte.");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var trainerMain = await userManager.FindByEmailAsync("trainer@demo.animalfitness.test");
        var trainerProfile = await db.TrainerProfiles.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.TenantId == tenant.Id && trainerMain != null && item.UserId == trainerMain.Id,
                cancellationToken);

        var plan = await db.WorkoutPlans.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.TenantId == tenant.Id && item.Name == "Fuerza base",
                cancellationToken);
        var planDay = plan is null
            ? null
            : await db.WorkoutDays.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(item => item.TenantId == tenant.Id && item.PlanId == plan.Id, cancellationToken);

        var extraTrainers = new (string Email, string First, string Last, int MonthsAgo)[]
        {
            ("entrenador02@demo.animalfitness.test", "Laura", "Gómez", 5),
            ("entrenador03@demo.animalfitness.test", "Carlos", "Ruiz", 3),
        };

        var athletes = new (string Email, string First, string Last, int MonthsAgo, decimal Height, decimal Weight)[]
        {
            ("alumno01@demo.animalfitness.test", "Camila", "Restrepo", 6, 162, 58),
            ("alumno02@demo.animalfitness.test", "Diego", "Vargas", 5, 178, 82),
            ("alumno03@demo.animalfitness.test", "Valentina", "Mejía", 5, 165, 61),
            ("alumno04@demo.animalfitness.test", "Andrés", "Pérez", 4, 175, 76),
            ("alumno05@demo.animalfitness.test", "Sofía", "Castro", 4, 160, 55),
            ("alumno06@demo.animalfitness.test", "Julián", "Hoyos", 3, 180, 88),
            ("alumno07@demo.animalfitness.test", "Mariana", "López", 3, 168, 64),
            ("alumno08@demo.animalfitness.test", "Felipe", "Ríos", 2, 172, 74),
            ("alumno09@demo.animalfitness.test", "Isabella", "Duque", 2, 158, 52),
            ("alumno10@demo.animalfitness.test", "Santiago", "Ortiz", 1, 176, 79),
            ("alumno11@demo.animalfitness.test", "Paula", "Cardona", 1, 163, 59),
            ("alumno12@demo.animalfitness.test", "Mateo", "Gil", 0, 181, 85),
        };

        var createdTrainerProfiles = new List<TrainerProfile>();
        foreach (var (email, first, last, monthsAgo) in extraTrainers)
        {
            var joined = now.AddMonths(-monthsAgo).AddDays(-3);
            var user = await CreateDemoUserAsync(userManager, db, email, first, last, "TrainerDemo1!", joined);
            db.TenantMemberships.Add(
                TenantMembership.Create(tenant.Id, user.Id, MembershipRole.Trainer, joined, null));
            var profile = TrainerProfile.Create(
                tenant.Id, user.Id, joined, null, $"{first} {last}", "Funcional, fuerza");
            db.TrainerProfiles.Add(profile);
            createdTrainerProfiles.Add(profile);
        }

        var athleteIndex = 0;
        foreach (var (email, first, last, monthsAgo, height, weight) in athletes)
        {
            var joined = now.AddMonths(-monthsAgo).AddDays(-(athleteIndex % 7));
            var user = await CreateDemoUserAsync(userManager, db, email, first, last, "AthleteDemo1!", joined);
            db.TenantMemberships.Add(
                TenantMembership.Create(tenant.Id, user.Id, MembershipRole.Athlete, joined, null));
            var athleteProfile = AthleteProfile.Create(
                tenant.Id, user.Id, joined, null, height, weight, notes: "Alumno demo histórico");
            db.AthleteProfiles.Add(athleteProfile);

            Guid? coachProfileId = null;
            if (trainerProfile is not null && athleteIndex % 3 != 2)
            {
                coachProfileId = trainerProfile.Id;
            }
            else if (createdTrainerProfiles.Count > 0)
            {
                coachProfileId = createdTrainerProfiles[athleteIndex % createdTrainerProfiles.Count].Id;
            }
            else if (trainerProfile is not null)
            {
                coachProfileId = trainerProfile.Id;
            }

            if (coachProfileId is Guid coachId)
            {
                db.TrainerAthletes.Add(
                    TrainerAthlete.Create(tenant.Id, coachId, athleteProfile.Id, joined.AddDays(1), null));
            }

            db.AthleteGoals.Add(AthleteGoal.Create(
                tenant.Id,
                athleteProfile.Id,
                athleteIndex % 2 == 0 ? "Mejorar fuerza" : "Bajar grasa",
                joined.AddDays(2),
                null,
                "Meta demo",
                athleteIndex % 2 == 0 ? 80 : 12,
                athleteIndex % 2 == 0 ? "kg" : "%"));

            // Mediciones mensuales desde el ingreso.
            for (var m = monthsAgo; m >= 0; m--)
            {
                var measuredAt = now.AddMonths(-m).AddDays(-1);
                if (measuredAt < joined)
                {
                    continue;
                }

                var drift = (monthsAgo - m) * 0.4m;
                db.BodyMeasurements.Add(BodyMeasurement.Create(
                    tenant.Id,
                    athleteProfile.Id,
                    measuredAt,
                    user.Id,
                    measuredAt,
                    weightKg: weight - drift,
                    heightCm: height,
                    bodyFatPercent: 22 - (drift * 0.3m),
                    waistCm: 80 - drift));
            }

            // Asistencia de los últimos ~10 semanas (algunas ausencias).
            for (var week = 0; week < 10; week++)
            {
                var day = DateOnly.FromDateTime(now.AddDays(-(week * 7) - (athleteIndex % 3)).UtcDateTime);
                var dayInstant = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                if (dayInstant < joined)
                {
                    continue;
                }

                var status = (week + athleteIndex) % 5 == 0
                    ? AttendanceStatus.Absent
                    : (week + athleteIndex) % 7 == 0
                        ? AttendanceStatus.Late
                        : AttendanceStatus.Present;
                db.AttendanceRecords.Add(AttendanceRecord.Create(
                    tenant.Id,
                    athleteProfile.Id,
                    day,
                    status,
                    trainerMain?.Id ?? user.Id,
                    dayInstant));
            }

            // Sesiones completadas espaciadas.
            if (plan is not null)
            {
                for (var s = 0; s < 4; s++)
                {
                    var started = now.AddDays(-(14 * (s + 1)) - athleteIndex).AddHours(17);
                    if (started < joined)
                    {
                        continue;
                    }

                    var session = WorkoutSession.Start(
                        tenant.Id,
                        athleteProfile.Id,
                        started,
                        user.Id,
                        plan.Id,
                        planDay?.Id,
                        "Sesión demo");
                    session.Complete(started.AddMinutes(55), user.Id);
                    db.WorkoutSessions.Add(session);
                }

                if (athleteIndex < 8)
                {
                    db.WorkoutPlanAssignments.Add(
                        WorkoutPlanAssignment.Create(
                            tenant.Id, plan.Id, athleteProfile.Id, joined.AddDays(3), trainerMain?.Id));
                }
            }

            athleteIndex++;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Roster histórico demo creado: {Trainers} entrenadores y {Athletes} alumnos (últimos meses).",
            extraTrainers.Length,
            athletes.Length);
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
