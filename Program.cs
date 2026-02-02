using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); // Limpiar proveedores de logging predeterminados
builder.Logging.AddConsole(); // Añadir proveedor de logging a consola

// CONEXIÓN
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// CAMBIO REALIZADO AQUÍ: Usamos una versión fija (8.0.21) en lugar de AutoDetect
// para evitar que la aplicación intente conectarse antes de entrar al bloque try-catch.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));

// IDENTITY
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

// NUCLEO DE INICIALIZACIÓN
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>(); // Obtener logger

    const int maxRetries = 5;
    const int delaySeconds = 5;

    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            logger.LogInformation($"Intento {i} de {maxRetries}: Aplicando migraciones e inicializando la base de datos.");
            // Aplicar migraciones automáticamente en todos los entornos
            context.Database.Migrate();

            await DbInitializer.Initialize(services);

            logger.LogInformation("Conexión exitosa a la base de datos y inicialización completada.");
            break; // Salir del bucle si es exitoso
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Intento {i} de {maxRetries} fallido. Error: {ex.Message}");

            if (i < maxRetries)
            {
                logger.LogWarning($"Esperando {delaySeconds} segundos antes de reintentar...");
                System.Threading.Thread.Sleep(delaySeconds * 1000); // Esperar en milisegundos
            }
            else
            {
                logger.LogError("FATAL ERROR: Fallaron todos los intentos de conectar e inicializar la base de datos.");
                throw; // Relanzar la excepción si todos los intentos fallaron
            }
        }
    }
}

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); }
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.Run();