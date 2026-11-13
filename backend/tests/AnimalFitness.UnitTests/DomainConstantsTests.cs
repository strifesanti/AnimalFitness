using AnimalFitness.Domain;

namespace AnimalFitness.UnitTests;

public sealed class DomainConstantsTests
{
    [Fact]
    public void Default_currency_is_colombian_peso()
    {
        Assert.Equal("COP", DomainConstants.DefaultCurrency);
    }

    [Fact]
    public void Default_culture_is_spanish_colombia()
    {
        Assert.Equal("es-CO", DomainConstants.DefaultCulture);
    }

    [Fact]
    public void Default_timezone_is_bogota()
    {
        Assert.Equal("America/Bogota", DomainConstants.DefaultTimeZoneId);
    }
}
