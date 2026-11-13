using AnimalFitness.Domain.Common;

namespace AnimalFitness.Domain.Accounts;

/// <summary>
/// Persona de la plataforma. Es global: puede pertenecer a varias organizaciones sin duplicar su cuenta.
/// Su identificador coincide con la cuenta de Identity. La contraseña y los sellos de seguridad
/// los gestiona el adaptador de identidad, no este tipo.
/// </summary>
public sealed class User : SoftDeletableEntity
{
    private User()
    {
    }

    private User(Guid id)
        : base(id)
    {
    }

    /// <summary>Correo normalizado. También es el nombre de usuario.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Nombres.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Apellidos.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Nombre completo para presentación.</summary>
    public string FullName => string.Concat(FirstName, " ", LastName);

    /// <summary>Teléfono.</summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>Indica si la cuenta puede iniciar sesión.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Indica si el correo ya fue confirmado.</summary>
    public bool EmailConfirmed { get; private set; }

    /// <summary>Último inicio de sesión, en UTC.</summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>
    /// Crea una cuenta activa con el correo todavía sin confirmar.
    /// </summary>
    /// <param name="email">Correo.</param>
    /// <param name="firstName">Nombres.</param>
    /// <param name="lastName">Apellidos.</param>
    /// <param name="now">Momento de creación, en UTC.</param>
    /// <param name="createdBy">Autor. Nulo cuando la persona se registra sola.</param>
    /// <param name="phoneNumber">Teléfono.</param>
    /// <param name="id">Identificador. Debe coincidir con la cuenta de Identity. Si se omite, se genera uno nuevo.</param>
    /// <returns>La cuenta creada.</returns>
    public static User Create(
        string email,
        string firstName,
        string lastName,
        DateTimeOffset now,
        Guid? createdBy = null,
        string? phoneNumber = null,
        Guid? id = null)
    {
        var user = id is null ? new User() : new User(id.Value);
        user.Email = TextRules.NormalizeEmail(email);
        user.FirstName = TextRules.PersonName(firstName, "El nombre");
        user.LastName = TextRules.PersonName(lastName, "El apellido");
        user.PhoneNumber = TextRules.OptionalPhone(phoneNumber);
        user.IsActive = true;
        user.EmailConfirmed = false;
        user.MarkCreated(now, createdBy);
        return user;
    }

    /// <summary>Cambia el nombre de la persona.</summary>
    /// <param name="firstName">Nombres.</param>
    /// <param name="lastName">Apellidos.</param>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void Rename(string firstName, string lastName, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        var normalizedFirstName = TextRules.PersonName(firstName, "El nombre");
        var normalizedLastName = TextRules.PersonName(lastName, "El apellido");
        MarkUpdated(now, actor);
        FirstName = normalizedFirstName;
        LastName = normalizedLastName;
    }

    /// <summary>Cambia o quita el teléfono.</summary>
    /// <param name="phoneNumber">Teléfono. Nulo lo elimina.</param>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que modifica.</param>
    public void ChangePhone(string? phoneNumber, DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        var normalizedPhone = TextRules.OptionalPhone(phoneNumber);
        MarkUpdated(now, actor);
        PhoneNumber = normalizedPhone;
    }

    /// <summary>Marca el correo como confirmado. Si ya lo estaba, no hace nada.</summary>
    /// <param name="now">Momento de la confirmación, en UTC.</param>
    /// <param name="actor">Usuario que confirma.</param>
    public void ConfirmEmail(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (EmailConfirmed)
        {
            return;
        }

        MarkUpdated(now, actor);
        EmailConfirmed = true;
    }

    /// <summary>Impide el inicio de sesión sin borrar la cuenta.</summary>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que desactiva.</param>
    public void Deactivate(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (!IsActive)
        {
            throw new DomainException("La cuenta ya está desactivada.");
        }

        MarkUpdated(now, actor);
        IsActive = false;
    }

    /// <summary>Vuelve a permitir el inicio de sesión.</summary>
    /// <param name="now">Momento de la modificación, en UTC.</param>
    /// <param name="actor">Usuario que activa.</param>
    public void Activate(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (IsActive)
        {
            throw new DomainException("La cuenta ya está activa.");
        }

        MarkUpdated(now, actor);
        IsActive = true;
    }

    /// <summary>Registra un inicio de sesión correcto.</summary>
    /// <param name="now">Momento del inicio de sesión, en UTC.</param>
    /// <param name="actor">Usuario que inicia sesión.</param>
    public void RecordLogin(DateTimeOffset now, Guid? actor)
    {
        EnsureNotDeleted();
        if (!IsActive)
        {
            throw new DomainException("Una cuenta desactivada no puede iniciar sesión.");
        }

        MarkUpdated(now, actor);
        LastLoginAt = now;
    }
}
