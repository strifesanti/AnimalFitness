namespace AnimalFitness.UnitTests;

internal static class TestClock
{
    public static readonly DateTimeOffset Now = new(2026, 11, 13, 14, 0, 0, TimeSpan.Zero);

    public static readonly Guid Actor = Guid.Parse("019a0000-0000-7000-8000-000000000001");
}
