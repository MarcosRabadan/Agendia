using Microsoft.OpenApi.Models;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Swagger UI con soporte para Bearer token (boton Authorize).
    /// </summary>
    public static class SwaggerSetup
    {
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Agendia API", Version = "v1" });

                // Surface XML doc summaries (endpoints + DTO schemas) in Swagger UI.
                // Each assembly emits its own XML next to the DLL (GenerateDocumentationFile).
                foreach (var xml in new[] { "MRC.Agendia.Api.xml", "MRC.Agendia.Application.xml" })
                {
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
                    if (File.Exists(xmlPath))
                    {
                        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                    }
                }

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Introduce el token JWT (sin el prefijo 'Bearer ')."
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            return services;
        }
    }
}
