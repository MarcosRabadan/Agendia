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

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseRateLimiter();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/health");

            return app;
        }
    }
}
