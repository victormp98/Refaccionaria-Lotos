using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. CONEXIÓN A BASE DE DATOS (MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ==========================================
// 2. CONFIGURACIÓN DE IDENTITY (BLINDADA)
// ==========================================
// Cambiamos AddDefaultIdentity por AddIdentity para soportar Roles sin errores.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI() // <--- IMPORTANTE: Esto carga las vistas de Login/Registro
.AddDefaultTokenProviders();

// 3. SERVICIOS MVC Y RAZOR PAGES
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // <--- Agregamos esto explícitamente para asegurar que las vistas de Identity carguen

// ==========================================
// 4. CONFIGURACIÓN DE SESIONES (CARRITO)
// ==========================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 5. INICIALIZADOR DE BASE DE DATOS (SEEDER)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await RefaccionariaWeb.Data.DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al crear los roles o datos iniciales.");
    }
}

// 6. PIPELINE HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Orden Importante: Auth -> Authorization -> Session
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// 7. RUTAS
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Necesario para que /Identity/Account/Register funcione



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>(); // Cambia 'YourDbContextName' por el nombre de tu Context
        context.Database.EnsureCreated();
        Console.WriteLine("✅ Base de datos verificada/creada con éxito.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al crear la base de datos: {ex.Message}");
    }
}
app.Run();