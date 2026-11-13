namespace AnimalFitness.Domain.Common;

/// <summary>
/// Límites de texto compartidos por el dominio y, más adelante, por la configuración de la base de datos.
/// </summary>
public static class FieldLengths
{
    /// <summary>Nombre o apellido de una persona.</summary>
    public const int PersonName = 80;

    /// <summary>Correo electrónico.</summary>
    public const int Email = 256;

    /// <summary>Teléfono.</summary>
    public const int Phone = 30;

    /// <summary>Identificador legible de una organización. También sirve como subdominio.</summary>
    public const int Slug = 63;

    /// <summary>Nombre de una organización.</summary>
    public const int OrganizationName = 120;

    /// <summary>Identificador de zona horaria.</summary>
    public const int TimeZoneId = 64;

    /// <summary>Código de moneda ISO 4217.</summary>
    public const int Currency = 3;

    /// <summary>Cultura, por ejemplo es-CO.</summary>
    public const int Locale = 10;

    /// <summary>Color hexadecimal, incluido el numeral.</summary>
    public const int Color = 7;

    /// <summary>Longitud máxima del hash de una invitación.</summary>
    public const int TokenHash = 128;

    /// <summary>Longitud mínima del hash de una invitación.</summary>
    public const int TokenHashMinimum = 32;

    /// <summary>Dirección IP, con espacio para IPv6.</summary>
    public const int IpAddress = 45;

    /// <summary>Agente de usuario.</summary>
    public const int UserAgent = 512;

    /// <summary>Acción registrada en la auditoría.</summary>
    public const int AuditAction = 100;

    /// <summary>Nombre del tipo de entidad auditado.</summary>
    public const int EntityType = 100;

    /// <summary>Motivo de rechazo de una solicitud.</summary>
    public const int RejectionReason = 500;

    /// <summary>Versión de una política de consentimiento.</summary>
    public const int PolicyVersion = 40;

    /// <summary>Configuración JSON de una organización.</summary>
    public const int SettingsJson = 8000;

    /// <summary>Notas del perfil de alumno.</summary>
    public const int ProfileNotes = 2000;

    /// <summary>Biografía del entrenador.</summary>
    public const int ProfileBio = 2000;

    /// <summary>Especialidades del entrenador.</summary>
    public const int ProfileSpecialties = 500;

    /// <summary>Título de un objetivo.</summary>
    public const int GoalTitle = 120;

    /// <summary>Descripción de un objetivo.</summary>
    public const int GoalDescription = 1000;

    /// <summary>Unidad de un objetivo.</summary>
    public const int GoalUnit = 20;

    /// <summary>Nombre de categoría o ejercicio.</summary>
    public const int ExerciseName = 120;

    /// <summary>Descripción corta de ejercicio o rutina.</summary>
    public const int ExerciseDescription = 2000;

    /// <summary>Grupo muscular o equipamiento.</summary>
    public const int MuscleOrEquipment = 80;

    /// <summary>Instrucciones de un ejercicio.</summary>
    public const int Instructions = 4000;

    /// <summary>URL de medio (imagen/video).</summary>
    public const int MediaUrl = 2048;

    /// <summary>Nombre de un plan o día de entrenamiento.</summary>
    public const int WorkoutName = 120;

    /// <summary>Notas de serie o sesión.</summary>
    public const int WorkoutNotes = 1000;

    /// <summary>Etiqueta de tempo (ej. 3-1-2).</summary>
    public const int Tempo = 20;

    /// <summary>Notas de una medición o foto de progreso.</summary>
    public const int ProgressNotes = 1000;

    /// <summary>Título de una valoración física.</summary>
    public const int AssessmentTitle = 120;

    /// <summary>Nombre de comida o reto.</summary>
    public const int NutritionOrChallengeName = 120;

    /// <summary>Descripción de reto o logro.</summary>
    public const int ChallengeDescription = 2000;

    /// <summary>Cuerpo de mensaje o notificación.</summary>
    public const int MessageBody = 4000;

    /// <summary>Título de notificación.</summary>
    public const int NotificationTitle = 160;
}
