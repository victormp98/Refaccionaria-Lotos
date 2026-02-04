using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURACIÓN DE LOGGING
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// CONEXIÓN A BASE DE DATOS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));

// IDENTITY
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false; // Ajusta según tus necesidades de seguridad
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// NUCLEO DE INICIALIZACIÓN (MIGRACIONES AUTOMÁTICAS)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 5;
    const int delaySeconds = 5;

    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            logger.LogInformation($"Intento {i} de {maxRetries}: Sincronizando Base de Datos y aplicando migraciones.");

            // APLICA TODAS LAS MIGRACIONES PENDIENTES (Incluye Pedidos, Detalles y SCRAP)
            context.Database.Migrate();

            // INICIALIZA DATOS (Roles, Admin, Sucursal, etc.)
            await DbInitializer.Initialize(services);

            logger.LogInformation("¡Base de datos sincronizada y tablas verificadas con éxito!");
            break;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Intento {i} fallido. Error: {ex.Message}");

            if (i < maxRetries)
            {
                logger.LogWarning($"Esperando {delaySeconds} segundos para reintentar...");
                Thread.Sleep(delaySeconds * 1000);
            }
            else
            {
                logger.LogError("FATAL ERROR: No se pudo sincronizar la base de datos después de varios intentos.");
            }
        }
    }
}

// CONFIGURACIÓN DEL PIPELINE DE HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();