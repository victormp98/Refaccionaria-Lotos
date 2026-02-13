using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Data
{
    public static class DbInitializer
    {
        public const string PUBLICO_GENERAL_EMAIL = "publico_general@refaccionaria.com";
        public static string PublicoGeneralUserId { get; private set; } // Para que el servicio pueda accederlo

        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // 1. ROLES (Nombres exactos)
            string[] roleNames = { "Admin", "Cliente", "Mostrador", "Almacen" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. ADMIN (Desde Coolify)
            var adminEmail = configuration["ADMIN_USER"];
            var adminPass = configuration["ADMIN_PASS"];

            if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPass))
            {
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

            // 3. USUARIO "PÚBLICO GENERAL" para ventas de mostrador
            var publicoGeneralUser = await userManager.FindByEmailAsync(PUBLICO_GENERAL_EMAIL);
            if (publicoGeneralUser == null)
            {
                publicoGeneralUser = new IdentityUser { UserName = PUBLICO_GENERAL_EMAIL, Email = PUBLICO_GENERAL_EMAIL, EmailConfirmed = true };
                // Se usa una contraseña fuerte por seguridad; el usuario nunca debería intentar iniciar sesión con ella.
                var result = await userManager.CreateAsync(publicoGeneralUser, "Publico123!"); 
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(publicoGeneralUser, "Cliente");
                }
                // Si la creación falla, el initializer podría lanzar una excepción o loggear.
                // Por ahora, asumimos que la creación será exitosa.
            }
            PublicoGeneralUserId = publicoGeneralUser.Id; // Guardamos el ID para acceso externo por el servicio
        }
    }
}
