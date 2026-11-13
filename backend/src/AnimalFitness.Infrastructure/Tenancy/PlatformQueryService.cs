using AnimalFitness.Application.Abstractions.Tenancy;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnimalFitness.Infrastructure.Tenancy;

/// <summary>
/// Única entrada permitida para consultar entidades de organización sin el filtro de tenant.
/// Cada uso queda registrado en el log con el motivo.
/// </summary>
public sealed class PlatformQueryService(
    AppDbContext dbContext,
    ILogger<PlatformQueryService> logger) : IPlatformQueryService
{
    /// <inheritdoc />
    public async Task<T> QueryWithoutTenantFilterAsync<T>(
        string reason,
        Func<IQueryableProvider, CancellationToken, Task<T>> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentNullException.ThrowIfNull(query);

        logger.LogWarning(
            "Consulta de plataforma sin filtro de organización. Motivo: {Reason}",
            reason);

        return await query(new DbQueryableProvider(dbContext), cancellationToken);
    }

    private sealed class DbQueryableProvider(AppDbContext dbContext) : IQueryableProvider
    {
        public IQueryable<TEntity> Query<TEntity>()
            where TEntity : class
            => dbContext.Set<TEntity>().IgnoreQueryFilters().AsNoTracking();
    }
}
