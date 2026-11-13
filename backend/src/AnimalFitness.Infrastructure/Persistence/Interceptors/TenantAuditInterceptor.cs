using AnimalFitness.Application.Abstractions;
using AnimalFitness.Domain.Auditing;
using AnimalFitness.Domain.Common;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AnimalFitness.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Aplica las reglas de escritura antes de cada <c>SaveChanges</c>:
/// <list type="bullet">
/// <item>Impide crear, mover o modificar registros de otra organización.</item>
/// <item>Convierte cualquier borrado de entidades con borrado lógico en un borrado lógico real.</item>
/// <item>Protege la auditoría: la fecha y el autor de creación no cambian, y los registros de auditoría no se editan.</item>
/// <item>Completa la fecha y el autor de modificación cuando el dominio no lo hizo.</item>
/// </list>
/// Un contexto sin organización activa puede escribir registros nuevos (procesos del sistema),
/// pero nunca modificar uno que no haya cargado con sus propias reglas de consulta.
/// </summary>
public sealed class TenantAuditInterceptor(IRequestContext requestContext, TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyRules(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var pending = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in pending)
        {
            if (entry.Entity is AuditLog)
            {
                if (entry.State != EntityState.Added)
                {
                    throw new InvalidOperationException("Los registros de auditoría son inmutables.");
                }

                continue;
            }

            if (entry.Entity is ITenantEntity)
            {
                EnsureTenantOwnership(entry);
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    FillCreationAudit(entry, now);
                    break;

                case EntityState.Modified:
                    EnsureAuditIsNotRewritten(entry);
                    FillUpdateAudit(entry, now);
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is not ISoftDeletable)
                    {
                        throw new InvalidOperationException(
                            $"{entry.Metadata.ClrType.Name} no admite borrado físico. Usa el método de dominio correspondiente.");
                    }

                    ConvertToSoftDelete(entry, now);
                    break;
            }
        }
    }

    private void EnsureTenantOwnership(EntityEntry entry)
    {
        var property = entry.Property(nameof(ITenantEntity.TenantId));
        var current = (Guid)property.CurrentValue!;

        if (entry.State == EntityState.Added)
        {
            if (current == Guid.Empty)
            {
                throw new TenantViolationException("El registro debe pertenecer a una organización.");
            }

            if (requestContext.TenantId is { } active && active != current)
            {
                throw new TenantViolationException("No se puede crear un registro en otra organización.");
            }

            return;
        }

        var original = (Guid)property.OriginalValue!;
        if (original != current)
        {
            throw new TenantViolationException("Un registro no puede cambiar de organización.");
        }

        if (requestContext.TenantId is { } active2 && original != active2)
        {
            throw new TenantViolationException("El registro pertenece a otra organización.");
        }
    }

    private void FillCreationAudit(EntityEntry entry, DateTimeOffset now)
    {
        if (entry.Entity is not IAuditable)
        {
            return;
        }

        if ((DateTimeOffset)entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue! == default)
        {
            entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
        }

        if (entry.Property(nameof(IAuditable.CreatedBy)).CurrentValue is null && requestContext.UserId is { } user)
        {
            entry.Property(nameof(IAuditable.CreatedBy)).CurrentValue = user;
        }
    }

    private void EnsureAuditIsNotRewritten(EntityEntry entry)
    {
        if (entry.Entity is BaseEntity)
        {
            if (entry.Property(nameof(BaseEntity.Id)).IsModified)
            {
                throw new InvalidOperationException("El identificador de un registro no puede cambiar.");
            }
        }

        if (entry.Entity is not IAuditable)
        {
            return;
        }

        if (entry.Property(nameof(IAuditable.CreatedAt)).IsModified
            || entry.Property(nameof(IAuditable.CreatedBy)).IsModified)
        {
            throw new InvalidOperationException("La fecha y el autor de creación no pueden modificarse.");
        }
    }

    private void FillUpdateAudit(EntityEntry entry, DateTimeOffset now)
    {
        if (entry.Entity is not IAuditable)
        {
            return;
        }

        if (entry.Property(nameof(IAuditable.UpdatedAt)).IsModified)
        {
            return;
        }

        entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
        entry.Property(nameof(IAuditable.UpdatedBy)).CurrentValue = requestContext.UserId;
    }

    private void ConvertToSoftDelete(EntityEntry entry, DateTimeOffset now)
    {
        entry.State = EntityState.Modified;
        entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;
        entry.Property(nameof(ISoftDeletable.DeletedAt)).CurrentValue = now;
        FillUpdateAudit(entry, now);
    }
}
