using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Services
{
    public class AlmacenService : IAlmacenService
    {
        private readonly ApplicationDbContext _context;

        public AlmacenService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Producto>> ObtenerTodosLosProductos(bool soloVisibles = false)
        {
            var query = _context.Productos.AsQueryable();
            query = query.Where(p => !p.Eliminado);

            if (soloVisibles)
            {
                query = query.Where(p => p.EsVisibleEnLinea == true);
            }

            return await query.ToListAsync();
        }

        public async Task<Producto?> ObtenerProductoPorId(int id)
        {
            return await _context.Productos
                .Include(p => p.Compatibilidades.Where(c => c.Vehiculo.Activo == true))
                .ThenInclude(c => c.Vehiculo)
                .FirstOrDefaultAsync(m => m.Id == id && !m.Eliminado);
        }

        public async Task<bool> ActualizarStock(int productoId, int cantidad, string motivo, string usuarioId)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null || producto.Eliminado)
            {
                return false;
            }

            if (cantidad < 0 && (producto.Stock + cantidad < 0))
            {
                return false;
            }

            producto.Stock += cantidad;

            var movimiento = new MovimientoInventario
            {
                ProductoId = productoId,
                TipoMovimiento = motivo,
                Cantidad = cantidad,
                FechaRegistro = System.DateTime.Now,
                UsuarioId = usuarioId,
                Referencia = motivo
            };

            _context.Update(producto);
            _context.MovimientosInventario.Add(movimiento);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Producto>> ObtenerProductosConStockBajo(int limite)
        {
            return await _context.Productos
                .Where(p => !p.Eliminado && p.Stock > 0 && p.Stock <= limite)
                .OrderBy(p => p.Stock)
                .ToListAsync();
        }

        public async Task<bool> MoverAPapelera(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return false;

            producto.Eliminado = true;
            _context.Update(producto);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestaurarDePapelera(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return false;

            producto.Eliminado = false;
            _context.Update(producto);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AlternarVisibilidadWeb(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return false;

            producto.EsVisibleEnLinea = !producto.EsVisibleEnLinea;
            _context.Update(producto);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Producto>> ObtenerPapelera()
        {
            return await _context.Productos.Where(p => p.Eliminado == true).ToListAsync();
        }

        public async Task<bool> CrearProducto(Producto producto)
        {
            _context.Add(producto);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EditarProducto(Producto producto)
        {
            try
            {
                _context.Update(producto);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Productos.Any(e => e.Id == producto.Id))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<bool> RegistrarCompra(int id, int cantidad, decimal pCompra, decimal pVenta, string usuarioId, string referencia)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null || producto.Eliminado)
            {
                return false;
            }

            if (cantidad <= 0)
            {
                return false;
            }

            // VALIDACIÓN DE NEGOCIO: Precio de venta no puede ser menor al de compra
            if (pVenta < pCompra)
            {
                return false;
            }

            producto.Stock += cantidad;
            producto.PrecioCompra = pCompra;
            producto.PrecioVenta = pVenta;

            var movimiento = new MovimientoInventario
            {
                ProductoId = id,
                TipoMovimiento = "ENTRADA",
                Cantidad = cantidad,
                FechaRegistro = System.DateTime.Now,
                Referencia = referencia ?? "Compra de mercancía",
                UsuarioId = usuarioId
            };

            _context.Update(producto);
            _context.Add(movimiento);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
