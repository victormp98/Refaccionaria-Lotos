using RefaccionariaWeb.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Services
{
    public interface IAlmacenService
    {
        Task<List<Producto>> ObtenerTodosLosProductos(bool soloVisibles = false, string buscar = null);
        Task<Producto?> ObtenerProductoPorId(int id);
        Task<bool> ActualizarStock(int productoId, int cantidad, string motivo, string usuarioId);
        Task<List<Producto>> ObtenerProductosConStockBajo(int limite);

        Task<bool> MoverAPapelera(int id);
        Task<bool> RestaurarDePapelera(int id);
        Task<bool> AlternarVisibilidadWeb(int id);
        Task<List<Producto>> ObtenerPapelera();

        Task<bool> CrearProducto(Producto producto);
        Task<bool> EditarProducto(Producto producto);
        Task<bool> RegistrarCompra(int id, int cantidad, decimal pCompra, decimal pVenta, string usuarioId, string referencia);
    }
}
