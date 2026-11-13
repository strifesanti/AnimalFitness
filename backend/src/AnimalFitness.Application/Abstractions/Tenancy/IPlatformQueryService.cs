namespace AnimalFitness.Application.Abstractions.Tenancy;

/// <summary>
/// Consultas de plataforma que necesitan saltar el filtro de organización.
/// Solo debe usarse en módulos SuperAdmin o en el arranque de sesión.
/// </summary>
public interface IPlatformQueryService
{
    /// <summary>
    /// Ejecuta una consulta sin el filtro de organización.
    /// </summary>
    /// <typeparam name="T">Tipo del resultado.</typeparam>
    /// <param name="reason">Motivo auditado del salto de filtro.</param>
    /// <param name="query">Consulta a ejecutar.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>El resultado de la consulta.</returns>
    Task<T> QueryWithoutTenantFilterAsync<T>(
        string reason,
        Func<IQueryableProvider, CancellationToken, Task<T>> query,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Proveedor de consultas sin filtro de organización. Solo lo crea <see cref="IPlatformQueryService"/>.
/// </summary>
public interface IQueryableProvider
{
    /// <summary>
    /// Obtiene un <see cref="IQueryable{T}"/> sin filtros globales.
    /// </summary>
    /// <typeparam name="TEntity">Tipo de entidad.</typeparam>
    IQueryable<TEntity> Query<TEntity>()
        where TEntity : class;
}
