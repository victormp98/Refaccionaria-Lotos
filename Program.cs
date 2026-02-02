using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); // Limpiar proveedores de logging predeterminados
builder.Logging.AddConsole(); // Añadir proveedor de logging a consola

// CONEXIÓN
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// CAMBIO REALIZADO AQUÍ: Usamos una versión fija (8.0.21)
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

            // 1. Aplicar migraciones existentes (si las hay)
            context.Database.Migrate();

            // ==============================================================================
            // PARCHE DE EMERGENCIA: CREAR TABLAS FALTANTES DIRECTAMENTE EN EL SERVIDOR
            // ==============================================================================
            logger.LogInformation("Ejecutando script de emergencia para asegurar tablas pedidos/detalles...");
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS pedidos (
                    Id INT NOT NULL AUTO_INCREMENT,
                    ClienteId VARCHAR(255) NOT NULL,
                    FechaPedido DATETIME(6) NOT NULL,
                    Status INT NOT NULL, 
                    TotalPedido DECIMAL(18,2) NOT NULL,
                    DireccionEnvio VARCHAR(200) NOT NULL,
                    CiudadEnvio VARCHAR(100) NOT NULL,
                    EstadoEnvio VARCHAR(100) NOT NULL,
                    CodigoPostalEnvio VARCHAR(10) NOT NULL,
                    PaisEnvio VARCHAR(100) NOT NULL DEFAULT 'México',
                    NombreReceptor VARCHAR(150) NOT NULL,
                    RequiereFactura BIT(1) NOT NULL,
                    Rfc VARCHAR(13) NULL,
                    RazonSocial VARCHAR(250) NULL,
                    PRIMARY KEY (Id),
                    CONSTRAINT FK_Pedidos_AspNetUsers_ClienteId FOREIGN KEY (ClienteId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
                ) CHARACTER SET utf8mb4;

                CREATE TABLE IF NOT EXISTS detallespedido (
                    Id INT NOT NULL AUTO_INCREMENT,
                    PedidoId INT NOT NULL,
                    ProductoId INT NOT NULL,
                    Cantidad INT NOT NULL,
                    PrecioUnitario DECIMAL(18,2) NOT NULL,
                    PRIMARY KEY (Id),
                    CONSTRAINT FK_DetallesPedido_Pedidos_PedidoId FOREIGN KEY (PedidoId) REFERENCES pedidos (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_DetallesPedido_Productos_ProductoId FOREIGN KEY (ProductoId) REFERENCES Productos (Id) ON DELETE CASCADE
                ) CHARACTER SET utf8mb4;
            ");
            // ==============================================================================
            // FIN DEL PARCHE
            // ==============================================================================

            await DbInitializer.Initialize(services);

            logger.LogInformation("Conexión exitosa, tablas verificadas e inicialización completada.");
            break; // Salir del bucle si es exitoso
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Intento {i} de {maxRetries} fallido. Error: {ex.Message}");

            if (i < maxRetries)
            {
                logger.LogWarning($"Esperando {delaySeconds} segundos antes de reintentar...");
                System.Threading.Thread.Sleep(delaySeconds * 1000);
            }
            else
            {
                logger.LogError("FATAL ERROR: Fallaron todos los intentos. La aplicación podría inestabilizarse.");
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