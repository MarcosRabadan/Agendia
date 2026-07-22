using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Api.Configuration;
using MRC.Agendia.Application;
using MRC.Agendia.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.ConfigureSerilog();

// MVC + utilities
builder.Services.AddControllers();
builder.Services.AddAppHealthChecks(builder.Configuration, builder.Environment);

// Cross-cutting (CORS, Swagger)
builder.Services
    .AddCorsForMobile(builder.Configuration, builder.Environment)
    .AddSwaggerWithJwt();

// App layers (each one self-registers)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Email sender (Logging in Dev/Test, SMTP elsewhere)
builder.Services.AddEmailSender(builder.Configuration, builder.Environment);

// Push sender (#51): only Logging in every environment for now (FCM pending)
builder.Services.AddPushSender(builder.Environment);

// JWT validation (tokens are issued by the Harmony identity service)
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

app.UseConfiguredPipeline();

// Auto-apply pending EF Core migrations in Development so a fresh clone
// or a teammate pulling a new migration does not have to run
// `dotnet ef database update` by hand. Skipped in Production - migrations
// belong to the deploy pipeline there. Skipped in Testing because those
// tests use the InMemory provider which does not support Migrate().
if (app.Environment.IsDevelopment())
{
    try
    {
        Log.Information("Agendia: comprobando migraciones EF Core pendientes...");
        using var migrationScope = app.Services.CreateScope();
        var db = migrationScope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            Log.Information("Agendia: base de datos al dia, sin migraciones pendientes.");
        }
        else
        {
            Log.Information("Agendia: aplicando {Count} migracion(es) pendiente(s): {Migrations}",
                pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync();
            Log.Information("Agendia: migraciones aplicadas correctamente.");
        }
    }
    catch (Exception ex)
    {
        // Re-throw: if the schema cannot be brought up to date the seed and
        // the API will fail anyway. Better to fail fast with a clear log.
        Log.Fatal(ex, "Error aplicando migraciones EF Core. La aplicacion no arrancara.");
        throw;
    }
}

// No role/admin seed: users and roles live in the Harmony identity service,
// which puts them in the token as claims. Agendia stores no credentials.

try
{
    Log.Information("Agendia Starting web application");
    app.Run();
}
catch (Exception ex)
{
    // Re-throw so the process exits with a non-zero code. A silent exit 0
    // would hide the crash from orchestrators and health probes.
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory<Program> in integration tests to be able
// to instantiate the host. Top-level Program is otherwise implicitly internal.
public partial class Program { }
