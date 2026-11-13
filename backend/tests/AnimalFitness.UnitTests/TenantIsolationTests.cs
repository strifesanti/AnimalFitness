using System.Reflection;
using AnimalFitness.Domain.Accounts;
using AnimalFitness.Domain.Auditing;
using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Compliance;
using AnimalFitness.Domain.Organizations;

namespace AnimalFitness.UnitTests;

public sealed class TenantIsolationTests
{
    [Fact]
    public void Tenant_id_cannot_be_reassigned_from_outside_the_entity()
    {
        var property = typeof(TenantScopedEntity).GetProperty(
            nameof(ITenantEntity.TenantId),
            BindingFlags.Instance | BindingFlags.Public);
        var setter = property!.GetSetMethod(nonPublic: true);

        Assert.NotNull(setter);
        Assert.True(setter!.IsPrivate);
    }

    [Fact]
    public void Global_people_and_organizations_do_not_carry_a_tenant_id()
    {
        Assert.Null(typeof(User).GetProperty("TenantId"));
        Assert.Null(typeof(Tenant).GetProperty("TenantId"));
        Assert.NotNull(typeof(TenantSettings).GetProperty(nameof(ITenantEntity.TenantId), BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void Records_with_an_optional_tenant_are_not_part_of_the_generic_filter()
    {
        Assert.Null(typeof(ConsentRecord).GetInterface(nameof(ITenantEntity)));
        Assert.Null(typeof(AuditLog).GetInterface(nameof(ITenantEntity)));
        Assert.NotNull(typeof(TenantMembership).GetInterface(nameof(ITenantEntity)));
    }
}
