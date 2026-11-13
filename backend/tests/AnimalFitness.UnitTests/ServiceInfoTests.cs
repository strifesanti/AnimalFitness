using AnimalFitness.Application;

namespace AnimalFitness.UnitTests;

public sealed class ServiceInfoTests
{
    [Fact]
    public void Current_identifies_the_product()
    {
        Assert.Equal("AnimalFitness", ServiceInfo.Current.Name);
        Assert.False(string.IsNullOrWhiteSpace(ServiceInfo.Current.Version));
    }
}
