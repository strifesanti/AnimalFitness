using AnimalFitness.API.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AnimalFitness.IntegrationTests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task Production_hides_exception_message()
    {
        var body = await ExecuteAsync(Environments.Production, "detalle-interno-secreto");

        Assert.Equal(StatusCodes.Status500InternalServerError, body.StatusCode);
        Assert.DoesNotContain("detalle-interno-secreto", body.Payload);
        Assert.Contains("Ocurrió un error inesperado.", body.Payload);
    }

    [Fact]
    public async Task Development_includes_exception_message()
    {
        var body = await ExecuteAsync(Environments.Development, "detalle-interno-secreto");

        Assert.Contains("detalle-interno-secreto", body.Payload);
    }

    private static async Task<(int StatusCode, string Payload)> ExecuteAsync(string environmentName, string message)
    {
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            new TestHostEnvironment { EnvironmentName = environmentName });

        var services = new ServiceCollection();
        services.AddLogging();
        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/v1/info";

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException(message),
            CancellationToken.None);

        Assert.True(handled);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var payload = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, payload);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "AnimalFitness.API";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
