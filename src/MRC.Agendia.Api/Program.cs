using MRC.Agendia.Api.Configuration;
using MRC.Agendia.Application;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Identity;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.ConfigureSerilog();

// MVC + utilidades
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Cross-cutting (CORS, rate limiting, Swagger)
builder.Services
    .AddCorsForMobile(builder.Configuration, builder.Environment)
    .AddAuthRateLimiting()
    .AddSwaggerWithJwt();

// Capas de la app (cada una se autorregistra)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Identity + JWT (depende de Infrastructure por el DbContext)
builder.Services.AddIdentityAndJwt(builder.Configuration);

var app = builder.Build();

app.UseConfiguredPipeline();

// Seed inicial de roles y admin
try
{
    Log.Information("Agendia: aplicando seed de roles y admin...");
    await DbInitializer.SeedRolesAndAdminAsync(app.Services);
    Log.Information("Agendia: seed completado.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Error durante el seed inicial");
}

try
{
    Log.Information("Agendia Starting web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory<Program> in integration tests to be able
// to instantiate the host. Top-level Program is otherwise implicitly internal.
public partial class Program { }
