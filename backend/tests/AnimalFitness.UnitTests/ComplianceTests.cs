using AnimalFitness.Domain.Auditing;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Compliance;

namespace AnimalFitness.UnitTests;

public sealed class ComplianceTests
{
    [Fact]
    public void Consent_can_be_revoked_once()
    {
        var consent = ConsentRecord.Grant(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            ConsentType.SensitiveHealthData,
            "2026-01",
            "190.0.0.1",
            TestClock.Now,
            TestClock.Actor);

        Assert.True(consent.IsActive);
        consent.Revoke(TestClock.Now.AddDays(1), consent.UserId);

        Assert.False(consent.IsActive);
        Assert.Throws<DomainException>(() => consent.Revoke(TestClock.Now.AddDays(2), consent.UserId));
        Assert.Equal(TestClock.Now.AddDays(1), consent.RevokedAt);
    }

    [Fact]
    public void Platform_consent_has_no_tenant()
    {
        var consent = ConsentRecord.Grant(
            Guid.CreateVersion7(),
            null,
            ConsentType.PersonalDataProcessing,
            "2026-01",
            null,
            TestClock.Now,
            null);

        Assert.Null(consent.TenantId);
    }

    [Fact]
    public void Audit_log_keeps_platform_events_outside_the_tenant_filter_contract()
    {
        var entry = AuditLog.Record(
            tenantId: null,
            userId: TestClock.Actor,
            action: "tenant.created",
            entityType: nameof(AuditLog),
            entityId: Guid.CreateVersion7(),
            changesJson: """{"currency":"COP"}""",
            ipAddress: "190.0.0.1",
            userAgent: "tests",
            occurredAt: TestClock.Now);

        Assert.Null(entry.TenantId);
        Assert.Equal("tenant.created", entry.Action);
        Assert.Throws<DomainException>(() => AuditLog.Record(
            Guid.Empty,
            TestClock.Actor,
            "tenant.created",
            "Tenant",
            null,
            null,
            null,
            null,
            TestClock.Now));
    }

    [Fact]
    public void Audit_log_rejects_json_that_is_not_an_object()
    {
        Assert.Throws<DomainException>(() => AuditLog.Record(
            Guid.CreateVersion7(),
            TestClock.Actor,
            "membership.removed",
            "TenantMembership",
            Guid.CreateVersion7(),
            """["no-es-objeto"]""",
            null,
            null,
            TestClock.Now));
    }
}
