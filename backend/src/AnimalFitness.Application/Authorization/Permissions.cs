namespace AnimalFitness.Application.Authorization;

/// <summary>
/// Permisos de la plataforma. Las políticas de la API usan estos nombres.
/// </summary>
public static class Permissions
{
    /// <summary>Administrar organizaciones a nivel plataforma.</summary>
    public const string TenantsManage = "Permissions.Tenants.Manage";

    /// <summary>Leer organizaciones a nivel plataforma.</summary>
    public const string TenantsRead = "Permissions.Tenants.Read";

    /// <summary>Editar datos de la organización activa.</summary>
    public const string OrganizationManage = "Permissions.Organization.Manage";

    /// <summary>Leer miembros de la organización.</summary>
    public const string MembersRead = "Permissions.Members.Read";

    /// <summary>Administrar miembros (rol, suspensión, baja).</summary>
    public const string MembersManage = "Permissions.Members.Manage";

    /// <summary>Crear y cancelar invitaciones.</summary>
    public const string InvitationsManage = "Permissions.Invitations.Manage";

    /// <summary>Revisar solicitudes de ingreso.</summary>
    public const string JoinRequestsManage = "Permissions.JoinRequests.Manage";

    /// <summary>Leer alumnos.</summary>
    public const string AthletesRead = "Permissions.Athletes.Read";

    /// <summary>Administrar alumnos y asignaciones.</summary>
    public const string AthletesManage = "Permissions.Athletes.Manage";

    /// <summary>Leer entrenadores.</summary>
    public const string TrainersRead = "Permissions.Trainers.Read";

    /// <summary>Administrar perfiles de entrenador.</summary>
    public const string TrainersManage = "Permissions.Trainers.Manage";

    /// <summary>Editar el propio perfil de usuario.</summary>
    public const string ProfileManageOwn = "Permissions.Profiles.ManageOwn";

    /// <summary>Leer biblioteca de ejercicios.</summary>
    public const string ExercisesRead = "Permissions.Exercises.Read";

    /// <summary>Administrar biblioteca de ejercicios.</summary>
    public const string ExercisesManage = "Permissions.Exercises.Manage";

    /// <summary>Leer planes / rutinas.</summary>
    public const string WorkoutsRead = "Permissions.Workouts.Read";

    /// <summary>Administrar planes / rutinas y asignaciones.</summary>
    public const string WorkoutsManage = "Permissions.Workouts.Manage";

    /// <summary>Leer sesiones (propias o de alumnos asignados).</summary>
    public const string SessionsRead = "Permissions.Sessions.Read";

    /// <summary>Registrar y cerrar sesiones propias.</summary>
    public const string SessionsManageOwn = "Permissions.Sessions.ManageOwn";

    /// <summary>Leer seguimiento físico (medidas, fotos, valoraciones, objetivos).</summary>
    public const string ProgressRead = "Permissions.Progress.Read";

    /// <summary>Registrar y editar seguimiento físico.</summary>
    public const string ProgressManage = "Permissions.Progress.Manage";

    /// <summary>Leer nutrición.</summary>
    public const string NutritionRead = "Permissions.Nutrition.Read";

    /// <summary>Administrar nutrición.</summary>
    public const string NutritionManage = "Permissions.Nutrition.Manage";

    /// <summary>Leer asistencia.</summary>
    public const string AttendanceRead = "Permissions.Attendance.Read";

    /// <summary>Registrar asistencia.</summary>
    public const string AttendanceManage = "Permissions.Attendance.Manage";

    /// <summary>Leer retos y rankings.</summary>
    public const string ChallengesRead = "Permissions.Challenges.Read";

    /// <summary>Administrar retos.</summary>
    public const string ChallengesManage = "Permissions.Challenges.Manage";

    /// <summary>Usar chat.</summary>
    public const string ChatUse = "Permissions.Chat.Use";

    /// <summary>Leer notificaciones.</summary>
    public const string NotificationsRead = "Permissions.Notifications.Read";
}
