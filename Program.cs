using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders(); // Limpiar proveedores de logging predeterminados
builder.Logging.AddConsole(); // Añadir proveedor de logging a consola

// CONEXIÓN
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

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

    try
    {
        // Aplicar migraciones automáticamente en todos los entornos
        context.Database.Migrate();

        await DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "FATAL ERROR: Ocurrió un error al migrar o inicializar la base de datos.");
        // Relanzamos la excepción para asegurar que Coolify registre el fallo y el contenedor no arranque.
        throw;
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