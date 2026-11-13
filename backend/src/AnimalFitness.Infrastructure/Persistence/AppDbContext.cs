using AnimalFitness.Application.Abstractions;
using AnimalFitness.Domain.Access;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Attendance;
using AnimalFitness.Domain.Auditing;
using AnimalFitness.Domain.Challenges;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Communication;
using AnimalFitness.Domain.Compliance;
using AnimalFitness.Domain.Nutrition;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Domain.Profiles;
using AnimalFitness.Domain.Tracking;
using AnimalFitness.Domain.Training;
using AnimalFitness.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Persistence;

/// <summary>
/// Contexto principal de datos. Combina las tablas de Identity con las entidades de dominio.
/// Las entidades de organización reciben un filtro global por <c>TenantId</c> y por borrado lógico.
/// Sin organización activa, el filtro no devuelve ninguna fila.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IRequestContext requestContext)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    /// <summary>Organizaciones. Son globales.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Perfiles de persona. Son globales.</summary>
    public DbSet<User> UserProfiles => Set<User>();

    /// <summary>Vínculos de personas con organizaciones.</summary>
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    /// <summary>Preferencias de cada organización.</summary>
    public DbSet<TenantSettings> OrganizationSettings => Set<TenantSettings>();

    /// <summary>Invitaciones emitidas por organizaciones.</summary>
    public DbSet<Invitation> Invitations => Set<Invitation>();

    /// <summary>Solicitudes de ingreso a organizaciones.</summary>
    public DbSet<JoinRequest> JoinRequests => Set<JoinRequest>();

    /// <summary>Autorizaciones de tratamiento de datos.</summary>
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();

    /// <summary>Historial inmutable de hechos relevantes.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>Perfiles de alumnos por organización.</summary>
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();

    /// <summary>Perfiles de entrenadores por organización.</summary>
    public DbSet<TrainerProfile> TrainerProfiles => Set<TrainerProfile>();

    /// <summary>Asignaciones entrenador–alumno.</summary>
    public DbSet<TrainerAthlete> TrainerAthletes => Set<TrainerAthlete>();

    /// <summary>Objetivos de alumnos.</summary>
    public DbSet<AthleteGoal> AthleteGoals => Set<AthleteGoal>();

    /// <summary>Categorías de ejercicios.</summary>
    public DbSet<ExerciseCategory> ExerciseCategories => Set<ExerciseCategory>();

    /// <summary>Biblioteca de ejercicios.</summary>
    public DbSet<Exercise> Exercises => Set<Exercise>();

    /// <summary>Medios de ejercicios.</summary>
    public DbSet<ExerciseMedia> ExerciseMediaItems => Set<ExerciseMedia>();

    /// <summary>Planes / rutinas.</summary>
    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();

    /// <summary>Días de un plan.</summary>
    public DbSet<WorkoutDay> WorkoutDays => Set<WorkoutDay>();

    /// <summary>Ejercicios prescritos en un día.</summary>
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();

    /// <summary>Asignaciones de planes a alumnos.</summary>
    public DbSet<WorkoutPlanAssignment> WorkoutPlanAssignments => Set<WorkoutPlanAssignment>();

    /// <summary>Sesiones ejecutadas.</summary>
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();

    /// <summary>Series registradas en sesión.</summary>
    public DbSet<ExerciseLog> ExerciseLogs => Set<ExerciseLog>();

    /// <summary>Mediciones corporales.</summary>
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();

    /// <summary>Fotografías de progreso.</summary>
    public DbSet<ProgressPhoto> ProgressPhotos => Set<ProgressPhoto>();

    /// <summary>Valoraciones físicas.</summary>
    public DbSet<FitnessAssessment> FitnessAssessments => Set<FitnessAssessment>();

    /// <summary>Objetivos nutricionales.</summary>
    public DbSet<NutritionTarget> NutritionTargets => Set<NutritionTarget>();

    /// <summary>Registros de comida.</summary>
    public DbSet<FoodEntry> FoodEntries => Set<FoodEntry>();

    /// <summary>Registros de agua.</summary>
    public DbSet<WaterLog> WaterLogs => Set<WaterLog>();

    /// <summary>Asistencia.</summary>
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    /// <summary>Retos.</summary>
    public DbSet<Challenge> Challenges => Set<Challenge>();

    /// <summary>Participantes de retos.</summary>
    public DbSet<ChallengeParticipant> ChallengeParticipants => Set<ChallengeParticipant>();

    /// <summary>Actividades de retos.</summary>
    public DbSet<ChallengeActivity> ChallengeActivities => Set<ChallengeActivity>();

    /// <summary>Logros.</summary>
    public DbSet<Achievement> Achievements => Set<Achievement>();

    /// <summary>Conversaciones de chat.</summary>
    public DbSet<Conversation> Conversations => Set<Conversation>();

    /// <summary>Participantes de chat.</summary>
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();

    /// <summary>Mensajes de chat.</summary>
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    /// <summary>Notificaciones in-app.</summary>
    public DbSet<AppNotification> AppNotifications => Set<AppNotification>();

    /// <summary>
    /// Organización activa. Si no hay ninguna, se usa <see cref="Guid.Empty"/>, que no coincide con ninguna fila.
    /// </summary>
    private Guid CurrentTenantId => requestContext.TenantId ?? Guid.Empty;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyTenantFilter<TenantMembership>(builder);
        ApplyTenantFilter<TenantSettings>(builder);
        ApplyTenantFilter<Invitation>(builder);
        ApplyTenantFilter<JoinRequest>(builder);
        ApplyTenantFilter<AthleteProfile>(builder);
        ApplyTenantFilter<TrainerProfile>(builder);
        ApplyTenantFilter<TrainerAthlete>(builder);
        ApplyTenantFilter<AthleteGoal>(builder);
        ApplyTenantFilter<ExerciseCategory>(builder);
        ApplyTenantFilter<Exercise>(builder);
        ApplyTenantFilter<ExerciseMedia>(builder);
        ApplyTenantFilter<WorkoutPlan>(builder);
        ApplyTenantFilter<WorkoutDay>(builder);
        ApplyTenantFilter<WorkoutExercise>(builder);
        ApplyTenantFilter<WorkoutPlanAssignment>(builder);
        ApplyTenantFilter<WorkoutSession>(builder);
        ApplyTenantFilter<ExerciseLog>(builder);
        ApplyTenantFilter<BodyMeasurement>(builder);
        ApplyTenantFilter<ProgressPhoto>(builder);
        ApplyTenantFilter<FitnessAssessment>(builder);
        ApplyTenantFilter<NutritionTarget>(builder);
        ApplyTenantFilter<FoodEntry>(builder);
        ApplyTenantFilter<WaterLog>(builder);
        ApplyTenantFilter<AttendanceRecord>(builder);
        ApplyTenantFilter<Challenge>(builder);
        ApplyTenantFilter<ChallengeParticipant>(builder);
        ApplyTenantFilter<ChallengeActivity>(builder);
        ApplyTenantFilter<Achievement>(builder);
        ApplyTenantFilter<Conversation>(builder);
        ApplyTenantFilter<ConversationParticipant>(builder);
        ApplyTenantFilter<ChatMessage>(builder);
        ApplyTenantFilter<AppNotification>(builder);

        if (Database.IsSqlServer())
        {
            // En SQL Server el token de concurrencia es un rowversion generado por la base de datos.
            foreach (var clrType in new[]
                     {
                         typeof(Tenant),
                         typeof(User),
                         typeof(TenantMembership),
                         typeof(TenantSettings),
                         typeof(Invitation),
                         typeof(JoinRequest),
                         typeof(ConsentRecord),
                         typeof(AthleteProfile),
                         typeof(TrainerProfile),
                         typeof(TrainerAthlete),
                         typeof(AthleteGoal),
                         typeof(ExerciseCategory),
                         typeof(Exercise),
                         typeof(ExerciseMedia),
                         typeof(WorkoutPlan),
                         typeof(WorkoutDay),
                         typeof(WorkoutExercise),
                         typeof(WorkoutPlanAssignment),
                         typeof(WorkoutSession),
                         typeof(ExerciseLog),
                         typeof(BodyMeasurement),
                         typeof(ProgressPhoto),
                         typeof(FitnessAssessment),
                         typeof(NutritionTarget),
                         typeof(FoodEntry),
                         typeof(WaterLog),
                         typeof(AttendanceRecord),
                         typeof(Challenge),
                         typeof(ChallengeParticipant),
                         typeof(ChallengeActivity),
                         typeof(Achievement),
                         typeof(Conversation),
                         typeof(ConversationParticipant),
                         typeof(ChatMessage),
                         typeof(AppNotification),
                     })
            {
                builder.Entity(clrType).Property(nameof(BaseEntity.RowVersion)).IsRowVersion();
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class
    {
        builder.Entity<TEntity>().HasQueryFilter(entity =>
            EF.Property<Guid>(entity, nameof(ITenantEntity.TenantId)) == CurrentTenantId
            && !EF.Property<bool>(entity, nameof(ISoftDeletable.IsDeleted)));
    }
}
