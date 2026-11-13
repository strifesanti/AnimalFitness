namespace AnimalFitness.UnitTests.Tenancy;

public sealed class IgnoreQueryFiltersGuardTests
{
    private static readonly string[] AllowedRelativePaths =
    [
        Path.Combine("src", "AnimalFitness.Infrastructure", "Tenancy", "TenantAccessService.cs"),
        Path.Combine("src", "AnimalFitness.Infrastructure", "Tenancy", "PlatformQueryService.cs"),
        Path.Combine("src", "AnimalFitness.Infrastructure", "Auth", "AuthService.cs"),
        Path.Combine("src", "AnimalFitness.Infrastructure", "Admin", "InvitationService.cs"),
        Path.Combine("src", "AnimalFitness.Infrastructure", "Admin", "JoinRequestService.cs"),
        Path.Combine("src", "AnimalFitness.Infrastructure", "Admin", "OrganizationService.cs"),
        Path.Combine("src", "AnimalFitness.Infrastructure", "Admin", "ProfileAdminService.cs"),
        Path.Combine("tests", "AnimalFitness.IntegrationTests", "Persistence", "TenantIsolationDatabaseTests.cs"),
        Path.Combine("tests", "AnimalFitness.UnitTests", "Tenancy", "IgnoreQueryFiltersGuardTests.cs"),
    ];

    [Fact]
    public void IgnoreQueryFilters_only_appears_in_allowed_files()
    {
        var backendRoot = FindBackendRoot();
        var offenders = Directory.EnumerateFiles(backendRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("IgnoreQueryFilters", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(backendRoot, path))
            .Where(relative => !AllowedRelativePaths.Contains(relative, StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "IgnoreQueryFilters solo puede usarse en servicios de tenancy/auth/admin autorizados y sus pruebas. Ofensores: "
            + string.Join(", ", offenders));
    }

    private static string FindBackendRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AnimalFitness.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se encontró AnimalFitness.sln.");
    }
}
