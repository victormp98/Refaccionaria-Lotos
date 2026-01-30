using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Models;

namespace RefaccionariaWeb.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Compatibilidad> Compatibilidades { get; set; }
    }
}
