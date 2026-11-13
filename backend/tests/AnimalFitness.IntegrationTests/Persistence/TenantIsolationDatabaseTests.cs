using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Auditing;
using AnimalFitness.Domain.Compliance;
using AnimalFitness.Domain.Organizations;
using AnimalFitness.Infrastructure.Persistence;
using AnimalFitness.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace AnimalFitness.IntegrationTests.Persistence;

public sealed class TenantIsolationDatabaseTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 14, 0, 0, TimeSpan.Zero);

    private readonly SqliteTestDatabase _database = new();

    private Guid _tenantA;
    private Guid _tenantB;
    private Guid _userId;

    public async Task InitializeAsync()
    {
        await _database.EnsureCreatedAsync();

        var tenantA = Tenant.Create("Academia A", "academia-a", "a@animalfitness.test", Now);
        var tenantB = Tenant.Create("Academia B", "academia-b", "b@animalfitness.test", Now);
        _tenantA = tenantA.Id;
        _tenantB = tenantB.Id;

        var accountId = Guid.CreateVersion7();
        _userId = accountId;

        await using var setup = _database.CreateContext();
        setup.Tenants.AddRange(tenantA, tenantB);
        setup.Users.Add(new ApplicationUser(accountId) { UserName = "ana@animalfitness.test", Email = "ana@animalfitness.test" });
        setup.UserProfiles.Add(User.Create("ana@animalfitness.test", "Ana", "Pérez", Now, id: accountId));
        await setup.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task Tenant_only_sees_its_own_memberships()
    {
        await using (var setup = _database.CreateContext())
        {
            setup.TenantMemberships.Add(TenantMembership.Create(_tenantA, _userId, MembershipRole.Trainer, Now, null));
            setup.TenantMemberships.Add(TenantMembership.Create(_tenantB, _userId, MembershipRole.Athlete, Now, null));
            await setup.SaveChangesAsync();
        }

        await using var fromA = _database.CreateContext(_tenantA);
        var visible = await fromA.TenantMemberships.ToListAsync();

        Assert.Single(visible);
        Assert.Equal(_tenantA, visible[0].TenantId);
        Assert.Equal(MembershipRole.Trainer, visible[0].Role);
    }

    [Fact]
    public async Task Without_an_active_tenant_no_organization_rows_are_visible()
    {
        await using (var setup = _database.CreateContext())
        {
            setup.TenantMemberships.Add(TenantMembership.Create(_tenantA, _userId, MembershipRole.Trainer, Now, null));
            await setup.SaveChangesAsync();
        }

        await using var anonymous = _database.CreateContext();

        Assert.Empty(await anonymous.TenantMemberships.ToListAsync());
        Assert.Equal(1, await anonymous.TenantMemberships.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Cannot_insert_a_membership_into_another_tenant()
    {
        await using var fromA = _database.CreateContext(_tenantA);
        fromA.TenantMemberships.Add(TenantMembership.Create(_tenantB, _userId, MembershipRole.Athlete, Now, null));

        await Assert.ThrowsAsync<TenantViolationException>(() => fromA.SaveChangesAsync());
    }

    [Fact]
    public async Task Cannot_move_a_membership_to_another_tenant()
    {
        Guid membershipId;
        await using (var setup = _database.CreateContext())
        {
            var membership = TenantMembership.Create(_tenantA, _userId, MembershipRole.Trainer, Now, null);
            membershipId = membership.Id;
            setup.TenantMemberships.Add(membership);
            await setup.SaveChangesAsync();
        }

        await using var fromA = _database.CreateContext(_tenantA);
        var loaded = await fromA.TenantMemberships.SingleAsync(m => m.Id == membershipId);
        fromA.Entry(loaded).Property(nameof(TenantMembership.TenantId)).CurrentValue = _tenantB;

        await Assert.ThrowsAsync<TenantViolationException>(() => fromA.SaveChangesAsync());
    }

    [Fact]
    public async Task Tenant_cannot_modify_a_membership_that_belongs_to_another_tenant()
    {
        Guid membershipId;
        await using (var setup = _database.CreateContext())
        {
            var membership = TenantMembership.Create(_tenantB, _userId, MembershipRole.Athlete, Now, null);
            membershipId = membership.Id;
            setup.TenantMemberships.Add(membership);
            await setup.SaveChangesAsync();
        }

        await using var fromA = _database.CreateContext(_tenantA);
        var hidden = await fromA.TenantMemberships.IgnoreQueryFilters().SingleAsync(m => m.Id == membershipId);
        hidden.Remove(Now.AddMinutes(1), null);

        await Assert.ThrowsAsync<TenantViolationException>(() => fromA.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_a_membership_is_a_soft_delete_and_hides_it()
    {
        Guid membershipId;
        await using (var setup = _database.CreateContext())
        {
            var membership = TenantMembership.Create(_tenantA, _userId, MembershipRole.Trainer, Now, null);
            membershipId = membership.Id;
            setup.TenantMemberships.Add(membership);
            await setup.SaveChangesAsync();
        }

        await using (var fromA = _database.CreateContext(_tenantA))
        {
            var toRemove = await fromA.TenantMemberships.SingleAsync(m => m.Id == membershipId);
            fromA.TenantMemberships.Remove(toRemove);
            await fromA.SaveChangesAsync();
        }


        await using var verify = _database.CreateContext(_tenantA);
        Assert.Empty(await verify.TenantMemberships.ToListAsync());

        var physical = await verify.TenantMemberships.IgnoreQueryFilters().SingleAsync(m => m.Id == membershipId);
        Assert.True(physical.IsDeleted);
        Assert.NotNull(physical.DeletedAt);
    }

    [Fact]
    public async Task Only_one_active_membership_per_person_and_tenant()
    {
        await using (var setup = _database.CreateContext())
        {
            setup.TenantMemberships.Add(TenantMembership.Create(_tenantA, _userId, MembershipRole.Trainer, Now, null));
            await setup.SaveChangesAsync();
        }

        await using var duplicate = _database.CreateContext();
        duplicate.TenantMemberships.Add(TenantMembership.Create(_tenantA, _userId, MembershipRole.Athlete, Now, null));

        await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
    }

    [Fact]
    public async Task A_removed_membership_allows_a_new_link_to_the_same_tenant()
    {
        await using (var setup = _database.CreateContext())
        {
            var first = TenantMembership.Create(_tenantA, _userId, MembershipRole.Athlete, Now, null);
            first.Remove(Now.AddDays(1), null);
            setup.TenantMemberships.Add(first);
            await setup.SaveChangesAsync();
        }

        await using var second = _database.CreateContext();
        second.TenantMemberships.Add(TenantMembership.Create(_tenantA, _userId, MembershipRole.Athlete, Now.AddDays(2), null));

        await second.SaveChangesAsync();

        await using var verify = _database.CreateContext(_tenantA);
        var rows = await verify.TenantMemberships.ToListAsync();

        // La membresía retirada se conserva como historial; solo una está activa.
        Assert.Equal(2, rows.Count);
        Assert.Single(rows, m => m.Status == MembershipStatus.Active);
    }

    [Fact]
    public async Task Audit_entries_cannot_be_changed()
    {
        await using var context = _database.CreateContext();
        var entry = AuditLog.Record(
            _tenantA,
            _userId,
            "membership.created",
            nameof(TenantMembership),
            Guid.CreateVersion7(),
            null,
            null,
            null,
            Now);
        context.AuditLogs.Add(entry);
        await context.SaveChangesAsync();

        var stored = await context.AuditLogs.SingleAsync(log => log.Id == entry.Id);
        context.Entry(stored).Property(nameof(AuditLog.Action)).CurrentValue = "membership.deleted";

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Consent_records_are_revoked_never_deleted()
    {
        Guid consentId;
        await using (var setup = _database.CreateContext())
        {
            var consent = ConsentRecord.Grant(
                _userId,
                _tenantA,
                ConsentType.SensitiveHealthData,
                "2026-01",
                "190.0.0.1",
                Now,
                _userId);
            consentId = consent.Id;
            setup.ConsentRecords.Add(consent);
            await setup.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        var stored = await context.ConsentRecords.SingleAsync(record => record.Id == consentId);
        context.ConsentRecords.Remove(stored);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Creation_timestamps_are_kept_and_updates_are_stamped()
    {
        Guid membershipId;
        await using (var setup = _database.CreateContext())
        {
            var membership = TenantMembership.Create(_tenantA, _userId, MembershipRole.Trainer, Now, _userId);
            membershipId = membership.Id;
            setup.TenantMemberships.Add(membership);
            await setup.SaveChangesAsync();
        }

        await using var context = _database.CreateContext(_tenantA, _userId);
        var loaded = await context.TenantMemberships.SingleAsync(m => m.Id == membershipId);
        loaded.Suspend(Now.AddHours(1), _userId);

        await context.SaveChangesAsync();

        Assert.Equal(Now, loaded.CreatedAt);
        Assert.Equal(_userId, loaded.CreatedBy);
        Assert.NotNull(loaded.UpdatedAt);
    }
}
