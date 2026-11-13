using System.Text.Json.Serialization;
using AnimalFitness.API.Admin;
using AnimalFitness.API.Advanced;
using AnimalFitness.API.Auth;
using AnimalFitness.API.Errors;
using AnimalFitness.API.Progress;
using AnimalFitness.API.Realtime;
using AnimalFitness.API.Tenancy;
using AnimalFitness.API.Training;
using AnimalFitness.Application;
using AnimalFitness.Infrastructure;
using AnimalFitness.Infrastructure.Seeding;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
    });

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddTenantResolution(builder.Configuration);
    builder.Services.AddAnimalFitnessAuth(builder.Environment);
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= context.HttpContext.Request.Path.Value;
            context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);

            if (context.ProblemDetails.Status == StatusCodes.Status404NotFound)
            {
                context.ProblemDetails.Title = "No se encontró el recurso.";
            }
        };
    });
    builder.Services
        .AddSignalR()
        .AddJsonProtocol(options =>
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddSingleton<ChatBroadcaster>();
    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsProduction() || app.Environment.IsStaging())
    {
        app.UseHsts();
    }

    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseHttpsRedirection();
    }

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<TenantResolutionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapHealthChecks("/health");
    app.MapGet("/api/v1/info", () => Results.Ok(ServiceInfo.Current))
        .WithName("GetServiceInfo")
        .WithTags("Sistema");
    app.MapAuthEndpoints();
    app.MapTenancyEndpoints();
    app.MapAdminEndpoints();
    app.MapTrainingEndpoints();
    app.MapProgressEndpoints();
    app.MapNutritionEndpoints();
    app.MapAttendanceEndpoints();
    app.MapChallengeEndpoints();
    app.MapChatEndpoints();
    app.MapNotificationEndpoints();
    app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

    if (app.Environment.IsDevelopment()
        && app.Configuration.GetValue("Seed:Enabled", defaultValue: true))
    {
        using var scope = app.Services.CreateScope();
        await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider);
    }

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "AnimalFitness.API se detuvo durante el arranque.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Punto de entrada de la API. La clase es pública para las pruebas de integración.
/// </summary>
public partial class Program
{
}
