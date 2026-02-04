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
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<Scrap> Scraps { get; set; }

        public DbSet<RefaccionariaWeb.Models.SucursalConfig> SucursalConfigs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call the base method for Identity tables

            // Explicitly map entity names to lowercase table names for MySQL compatibility on Linux
            modelBuilder.Entity<Producto>().ToTable("productos");
            modelBuilder.Entity<Vehiculo>().ToTable("vehiculos");
            modelBuilder.Entity<Compatibilidad>().ToTable("compatibilidades");
            modelBuilder.Entity<Pedido>().ToTable("pedidos");
            modelBuilder.Entity<DetallePedido>().ToTable("detallespedido");
            modelBuilder.Entity<Scrap>().ToTable("scraps");
            // For Identity tables, typically handled by base.OnModelCreating, but if issues arise,
            // they can be mapped explicitly as well:
            // modelBuilder.Entity<IdentityUser>().ToTable("aspnetusers");
            // modelBuilder.Entity<IdentityRole>().ToTable("aspnetroles");
            // ... and so on for other Identity tables
        }
    }
}
