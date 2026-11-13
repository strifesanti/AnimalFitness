using AnimalFitness.Application.Authorization;
using AnimalFitness.Application.Common;
using AnimalFitness.Application.Features.Attendance;
using AnimalFitness.Domain.Attendance;
using AnimalFitness.Domain.Common;
using AnimalFitness.Infrastructure.Admin;
using AnimalFitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.Infrastructure.Advanced;

/// <summary>Asistencia de alumnos (Fase 5).</summary>
public sealed class AttendanceService(
    AppDbContext db,
    AdminAccess access,
    AthleteResolver athletes,
    TimeProvider timeProvider) : IAttendanceService
{
    private const int MaxRangeDays = 366;
    private const int DefaultRangeDays = 30;
    private const int MaxListed = 2000;

    /// <inheritdoc />
    public async Task<AppResult> ListAsync(
        Guid? athleteProfileId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var target = await athletes.ForReadAsync(Permissions.AttendanceRead, athleteProfileId, true, cancellationToken);
        if (target.Failure is not null)
        {
            return target.Failure;
        }

        var (start, end, rangeFailure) = ResolveRange(from, to);
        if (rangeFailure is not null)
        {
            return rangeFailure;
        }

        if (!target.IsAll && target.AthleteId is null)
        {
            return new AppResult.Success<IReadOnlyList<AttendanceRecordDto>>([]);
        }

        var records = await LoadAsync(target, start, end, cancellationToken);
        var names = await athletes.NamesAsync(
            records.Select(item => item.AthleteProfileId).Distinct().ToList(),
            cancellationToken);

        var items = records
            .OrderByDescending(item => item.AttendanceDate)
            .Take(MaxListed)
            .Select(item => new AttendanceRecordDto(
                item.Id,
                item.AthleteProfileId,
                names.GetValueOrDefault(item.AthleteProfileId, string.Empty),
                item.AttendanceDate,
                item.Status,
                item.Notes,
                item.RecordedByUserId))
            .ToList();
        return new AppResult.Success<IReadOnlyList<AttendanceRecordDto>>(items);
    }

    /// <inheritdoc />
    public async Task<AppResult> UpsertAsync(UpsertAttendanceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var target = await athletes.ForWriteAsync(Permissions.AttendanceManage, request.AthleteProfileId, false, cancellationToken);
        if (target.Failure is not null || target.AthleteId is not { } id)
        {
            return target.Failure ?? AdminAccess.Fail("No se encontró el alumno.", 404);
        }

        var now = timeProvider.GetUtcNow();
        var actor = access.RequireUserId();
        var existing = await db.AttendanceRecords.FirstOrDefaultAsync(
            item => item.AthleteProfileId == id && item.AttendanceDate == request.Date,
            cancellationToken);

        AttendanceRecord record;
        try
        {
            if (existing is null)
            {
                record = AttendanceRecord.Create(
                    access.RequireTenantId(),
                    id,
                    request.Date,
                    request.Status,
                    actor,
                    now,
                    request.Notes);
                db.AttendanceRecords.Add(record);
            }
            else
            {
                existing.Update(request.Status, request.Notes, now, actor);
                record = existing;
            }
        }
        catch (DomainException ex)
        {
            return AdminAccess.Fail(ex.Message, 400);
        }

        await db.SaveChangesAsync(cancellationToken);
        var names = await athletes.NamesAsync([id], cancellationToken);
        return new AppResult.Success<AttendanceRecordDto>(
            new AttendanceRecordDto(
                record.Id,
                record.AthleteProfileId,
                names.GetValueOrDefault(id, string.Empty),
                record.AttendanceDate,
                record.Status,
                record.Notes,
                record.RecordedByUserId),
            "Asistencia guardada.");
    }

    /// <inheritdoc />
    public async Task<AppResult> GetStatsAsync(
        Guid? athleteProfileId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var target = await athletes.ForReadAsync(Permissions.AttendanceRead, athleteProfileId, true, cancellationToken);
        if (target.Failure is not null)
        {
            return target.Failure;
        }

        var (start, end, rangeFailure) = ResolveRange(from, to);
        if (rangeFailure is not null)
        {
            return rangeFailure;
        }

        var records = !target.IsAll && target.AthleteId is null
            ? new List<AttendanceRecord>()
            : await LoadAsync(target, start, end, cancellationToken);
        return new AppResult.Success<AttendanceStatsDto>(
            ComputeStats(target.IsAll ? null : target.AthleteId, start, end, records.Select(item => item.Status)));
    }

    /// <summary>Calcula conteos y porcentajes. Pública para poder probarla sin base de datos.</summary>
    public static AttendanceStatsDto ComputeStats(
        Guid? athleteProfileId,
        DateOnly from,
        DateOnly to,
        IEnumerable<AttendanceStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        var list = statuses.ToList();
        var present = list.Count(status => status == AttendanceStatus.Present);
        var late = list.Count(status => status == AttendanceStatus.Late);
        var absent = list.Count(status => status == AttendanceStatus.Absent);
        var excused = list.Count(status => status == AttendanceStatus.Excused);
        var total = list.Count;
        return new AttendanceStatsDto(
            athleteProfileId,
            from,
            to,
            total,
            present,
            late,
            absent,
            excused,
            total == 0 ? 0 : Math.Round(present * 100m / total, 1),
            total == 0 ? 0 : Math.Round((present + late) * 100m / total, 1));
    }

    private async Task<List<AttendanceRecord>> LoadAsync(
        AthleteTarget target,
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken)
    {
        var query = db.AttendanceRecords.AsNoTracking()
            .Where(item => item.AttendanceDate >= start && item.AttendanceDate <= end);
        if (!target.IsAll && target.AthleteId is { } id)
        {
            query = query.Where(item => item.AthleteProfileId == id);
        }

        return await query.ToListAsync(cancellationToken);
    }

    private (DateOnly Start, DateOnly End, AppResult? Failure) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var end = to ?? today;
        var start = from ?? end.AddDays(-DefaultRangeDays);
        if (start > end)
        {
            return (start, end, AdminAccess.Fail("El rango de fechas no es válido.", 400));
        }

        if (end.DayNumber - start.DayNumber > MaxRangeDays)
        {
            return (start, end, AdminAccess.Fail("El rango máximo es de un año.", 400));
        }

        return (start, end, null);
    }
}
