using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace RefaccionariaWeb.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // 1. Roles exactos de tu proyecto
            string[] roleNames = { "Admin", "Cliente", "Mostrador", "Almacen" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Crear Admin desde Variables de Entorno (SEGURIDAD)
            var adminEmail = configuration["ADMIN_USER"] ?? "admin@lotos.com";
            var adminPass = configuration["ADMIN_PASS"] ?? "Lotos2026!"; // Password por defecto si olvidas ponerla en Coolify

            var user = await userManager.FindByEmailAsync(adminEmail);
            if (user == null)
            {
                var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(admin, adminPass);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}