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
        }
    }
}