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
            // 4. PRODUCTOS DE PRUEBA (SEED DATA PARA PRUEBAS E2E LOCALES)
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            if (!context.Productos.Any())
            {
                context.Productos.AddRange(
                    new Producto
                    {
                        Nombre = "Aceite Sintético 5W-30 Motorcraft 5L",
                        Descripcion = "Aceite de motor sintético avanzado para un rendimiento superior. (Num. Parte: XO-5W30-Q1SP)",
                        MarcaPieza = "Motorcraft",
                        SKU = "ACE-MOT-5W30",
                        PrecioCompra = 450.00m,
                        PrecioVenta = 750.00m,
                        Stock = 50,
                        Pasillo = "A1",
                        Anaquel = "E3",
                        ImagenUrl = null
                    },
                    new Producto
                    {
                        Nombre = "Batería LTH AGM 35/85",
                        Descripcion = "Batería para auto Start-Stop de alto rendimiento. (Num. Parte: AGM-35-85)",
                        MarcaPieza = "LTH",
                        SKU = "BAT-LTH-AGM",
                        PrecioCompra = 1800.00m,
                        PrecioVenta = 2650.00m,
                        Stock = 15,
                        Pasillo = "Piso",
                        Anaquel = "Zona Baterías",
                        ImagenUrl = null
                    },
                    new Producto
                    {
                        Nombre = "Balatas Delanteras de Cerámica Wagner",
                        Descripcion = "Juego de balatas cerámicas sin ruido para Mazda 3 2014-2018. (Num. Parte: QC1624)",
                        MarcaPieza = "Wagner",
                        SKU = "BAL-WAG-QC1624",
                        PrecioCompra = 350.00m,
                        PrecioVenta = 690.00m,
                        Stock = 30,
                        Pasillo = "B2",
                        Anaquel = "E1",
                        ImagenUrl = null
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
