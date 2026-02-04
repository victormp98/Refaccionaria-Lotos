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
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<RefaccionariaWeb.Models.SucursalConfig> SucursalConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Producto>().ToTable("productos");
            modelBuilder.Entity<Vehiculo>().ToTable("vehiculos");
            modelBuilder.Entity<Compatibilidad>().ToTable("compatibilidades");
            modelBuilder.Entity<Pedido>().ToTable("pedidos");
            modelBuilder.Entity<DetallePedido>().ToTable("detallespedido");
            modelBuilder.Entity<Scrap>().ToTable("scraps");
        }
    }
}