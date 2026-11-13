namespace AnimalFitness.Domain.Communication;

/// <summary>Tipo de notificación in-app.</summary>
public enum NotificationKind
{
    /// <summary>Genérica.</summary>
    General = 1,

    /// <summary>Mensaje de chat.</summary>
    ChatMessage = 2,

    /// <summary>Reto.</summary>
    Challenge = 3,

    /// <summary>Solicitud / membresía.</summary>
    Membership = 4,

    /// <summary>Entrenamiento / rutina.</summary>
    Workout = 5,

    /// <summary>Recordatorio.</summary>
    Reminder = 6,
}
