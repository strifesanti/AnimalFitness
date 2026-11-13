namespace AnimalFitness.Domain.Compliance;

/// <summary>
/// Tipo de autorización que una persona otorga sobre sus datos.
/// </summary>
public enum ConsentType
{
    /// <summary>Tratamiento de datos personales de identificación y contacto.</summary>
    PersonalDataProcessing = 1,

    /// <summary>Datos corporales y de salud, tratados como datos sensibles.</summary>
    SensitiveHealthData = 2,

    /// <summary>Fotografías de evolución física.</summary>
    ProgressPhotos = 3,

    /// <summary>Mensajes comerciales y recordatorios opcionales.</summary>
    MarketingCommunications = 4,
}
