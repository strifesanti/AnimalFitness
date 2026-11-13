using AnimalFitness.Domain.Common;
using AnimalFitness.Domain.Organizations;

namespace AnimalFitness.UnitTests;

public sealed class OrganizationTests
{
    [Fact]
    public void Create_uses_colombian_defaults_and_normalizes_the_slug()
    {
        var tenant = Tenant.Create(" Acción   Total ", "Acción-Total", "Admin@AnimalFitness.test", TestClock.Now);

        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("Acción Total", tenant.Name);
        Assert.Equal("accion-total", tenant.Slug);
        Assert.Equal("admin@animalfitness.test", tenant.ContactEmail);
        Assert.Equal(TenantStatus.Trial, tenant.Status);
        Assert.Equal("COP", tenant.Currency);
        Assert.Equal("America/Bogota", tenant.TimeZoneId);
        Assert.Equal(TestClock.Now, tenant.CreatedAt);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("api")]
    [InlineData("academia_animal")]
    [InlineData("-academia")]
    public void Create_rejects_invalid_slug(string slug)
    {
        Assert.Throws<DomainException>(() =>
            Tenant.Create("Academia", slug, "admin@animalfitness.test", TestClock.Now));
    }

    [Fact]
    public void Suspend_and_activate_follow_the_allowed_transitions()
    {
        var tenant = Tenant.Create("Academia", "academia", "admin@animalfitness.test", TestClock.Now);
        var later = TestClock.Now.AddMinutes(5);

        tenant.Suspend(later, TestClock.Actor);
        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        Assert.Throws<DomainException>(() => tenant.Suspend(later.AddMinutes(1), TestClock.Actor));

        tenant.Activate(later.AddMinutes(2), TestClock.Actor);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Throws<DomainException>(() => tenant.Activate(later.AddMinutes(3), TestClock.Actor));
    }

    [Fact]
    public void Deleted_tenant_cannot_be_suspended()
    {
        var tenant = Tenant.Create("Academia", "academia", "admin@animalfitness.test", TestClock.Now);
        tenant.SoftDelete(TestClock.Now.AddMinutes(1), TestClock.Actor);

        Assert.True(tenant.IsDeleted);
        Assert.Throws<DomainException>(() => tenant.Suspend(TestClock.Now.AddMinutes(2), TestClock.Actor));
        Assert.Equal(TenantStatus.Trial, tenant.Status);
    }

    [Fact]
    public void Settings_start_with_the_brand_defaults_and_reject_invalid_json()
    {
        var tenantId = Guid.CreateVersion7();
        var settings = TenantSettings.Create(tenantId, TestClock.Now, TestClock.Actor);

        Assert.Equal(tenantId, settings.TenantId);
        Assert.Equal("#7C3AED", settings.PrimaryColor);
        Assert.Equal("es-CO", settings.Locale);
        Assert.Equal("COP", settings.Currency);

        var act = () => settings.Update("#c6f135", "es-co", "usd", "[1, 2]", TestClock.Now.AddMinutes(1), TestClock.Actor);
        Assert.Throws<DomainException>(act);
        Assert.Equal("{}", settings.SettingsJson);

        settings.Update("#112233", "es-co", "cop", """{"weekStart":"monday"}""", TestClock.Now.AddMinutes(1), TestClock.Actor);
        Assert.Equal("#112233", settings.PrimaryColor);
        Assert.Equal("es-CO", settings.Locale);
        Assert.Equal("""{"weekStart":"monday"}""", settings.SettingsJson);
    }
}
