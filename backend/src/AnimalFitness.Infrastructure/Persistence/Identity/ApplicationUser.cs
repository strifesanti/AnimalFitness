using Microsoft.AspNetCore.Identity;

namespace AnimalFitness.Infrastructure.Persistence.Identity;

/// <summary>
/// Cuenta de inicio de sesión (tabla AspNetUsers). Guarda credenciales y sellos de seguridad.
/// Su identificador es el mismo que el de <c>UserProfiles</c>, el perfil de dominio.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Crea una cuenta con identificador nuevo. Lo necesita Identity para materializar filas.
    /// </summary>
    public ApplicationUser()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Crea una cuenta con un identificador conocido.
    /// </summary>
    /// <param name="id">Identificador compartido con el perfil de dominio.</param>
    public ApplicationUser(Guid id)
    {
        Id = id;
    }
}
