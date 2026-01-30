using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RefaccionariaWeb.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            // Obtenemos el gestor de Roles
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Definimos los nombres de los puestos de trabajo
            string[] roleNames = { "Admin", "Cliente", "Mostrador", "Almacen" };

            // Recorremos la lista y creamos los que falten
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    // Crea el rol en la tabla AspNetRoles
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
    }
}