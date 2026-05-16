using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Infrastructure.Identity
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AgendiaDbContext>>();

            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Rol creado: {Role}", role);
                }
            }

            var adminSection = configuration.GetSection("AdminSeed");
            var adminEmail = adminSection["Email"];
            var adminPassword = adminSection["Password"];
            var adminFullName = adminSection["FullName"] ?? "Administrador";

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                logger.LogWarning("AdminSeed no configurado. No se crea usuario admin inicial.");
                return;
            }

            var existing = await userManager.FindByEmailAsync(adminEmail);
            if (existing is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = adminFullName,
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, Roles.Admin);
                    logger.LogInformation("Usuario admin creado: {Email}", adminEmail);
                }
                else
                {
                    logger.LogError("Error al crear admin: {Errors}",
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
