using MRC.Agendia.Api.Middleware;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Configura el HTTP request pipeline en el orden correcto.
    ///
    /// Orden critico:
    ///   1. HttpsRedirection
    ///   2. CORS
    ///   3. RateLimiter
    ///   4. ExceptionHandlingMiddleware
    ///   5. Authentication
    ///   6. Authorization
    ///   7. Controllers
    /// </summary>
    public static class PipelineExtensions
    {
        public static WebApplication UseConfiguredPipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Agendia API v1");
                    c.RoutePrefix = string.Empty;
                });
            }

            // Skipped under "Testing" so WebApplicationFactory does not have to follow
            // 307 redirects when calling the API over TestServer.
            if (!app.Environment.IsEnvironment("Testing"))
            {
                app.UseHttpsRedirection();
            }

            app.UseCors();

            // Skipped under "Testing" because the auth rate limits (login 5/min,
            // register 3/h) collide with integration scenarios that exercise the
            // lockout-after-5-failures path or register several users in a row.
            if (!app.Environment.IsEnvironment("Testing"))
            {
                app.UseRateLimiter();
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/health");

            return app;
        }
    }
}
