using Microsoft.EntityFrameworkCore;
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
builder.Services.AddAppHealthChecks(builder.Configuration, builder.Environment);

// Cross-cutting (CORS, rate limiting, Swagger)
builder.Services
    .AddCorsForMobile(builder.Configuration, builder.Environment)
    .AddAuthRateLimiting()
    .AddSwaggerWithJwt();

// Capas de la app (cada una se autorregistra)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Email sender (Logging en Dev/Test, SMTP en el resto)
builder.Services.AddEmailSender(builder.Configuration, builder.Environment);

// Identity + JWT (depende de Infrastructure por el DbContext)
builder.Services.AddIdentityAndJwt(builder.Configuration);

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

// Seed inicial de roles y admin
try
{
    Log.Information("Agendia: aplicando seed de roles y admin...");
    await DbInitializer.SeedRolesAndAdminAsync(app.Services);
    Log.Information("Agendia: seed completado.");
}
catch (Exception ex)
{
    // Re-throw: without the roles/admin seed the Admin-only endpoints are
    // unusable, so fail fast with a clear log instead of starting broken.
    Log.Fatal(ex, "Error durante el seed inicial. La aplicacion no arrancara.");
    throw;
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
