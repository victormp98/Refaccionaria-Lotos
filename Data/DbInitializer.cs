using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RefaccionariaWeb.Models;

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

            // 2. ADMIN (Desde Coolify o Fallback local)
            var adminEmail = configuration["ADMIN_USER"] ?? "admin@refaccionaria.com";
            var adminPass = configuration["ADMIN_PASS"] ?? "Admin_12345!";

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
                else
                {
                    // MODO RESCATE: Si el usuario ya existe pero olvidaste la contraseña, 
                    // la forzamos a que sea la del archivo de configuración/fallback.
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    await userManager.ResetPasswordAsync(user, token, adminPass);
                }
            }

            // 3. USUARIO "PÚBLICO GENERAL" para ventas de mostrador
            var publicoGeneralUser = await userManager.FindByEmailAsync(PUBLICO_GENERAL_EMAIL);
            if (publicoGeneralUser == null)
            {
                publicoGeneralUser = new IdentityUser { UserName = PUBLICO_GENERAL_EMAIL, Email = PUBLICO_GENERAL_EMAIL, EmailConfirmed = true };
                // Se genera una contraseña fuerte aleatoria; el usuario nunca debería intentar iniciar sesión con ella.
                string randomPassword = Guid.NewGuid().ToString() + "A1!";
                var result = await userManager.CreateAsync(publicoGeneralUser, randomPassword); 
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
