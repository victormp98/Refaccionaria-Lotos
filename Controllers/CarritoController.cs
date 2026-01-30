using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Extensions;
using RefaccionariaWeb.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Controllers
{
    [Authorize]
    public class CarritoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CarritoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var carritoSesion = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito") ?? new List<ItemCarrito>();
            bool huboCambios = false;

            // VALIDACIÓN DE STOCK EN TIEMPO REAL
            foreach (var item in carritoSesion)
            {
                var productoReal = await _context.Productos.FindAsync(item.ProductoId);

                if (productoReal == null)
                {
                    item.EsValido = false;
                    item.MensajeError = "Producto descontinuado.";
                    item.Cantidad = 0;
                    huboCambios = true;
                }
                else if (productoReal.Stock == 0)
                {
                    item.EsValido = false;
                    item.MensajeError = "¡Agotado! Alguien te lo ganó.";
                    item.StockMaximo = 0;
                    huboCambios = true;
                }
                else if (item.Cantidad > productoReal.Stock)
                {
                    item.EsValido = true;
                    item.MensajeError = $"Stock reducido. Solo quedan {productoReal.Stock}.";
                    item.Cantidad = productoReal.Stock;
                    item.StockMaximo = productoReal.Stock;
                    huboCambios = true;
                }
                else
                {
                    item.EsValido = true;
                    item.MensajeError = null;
                    item.StockMaximo = productoReal.Stock;
                }
            }

            if (huboCambios)
            {
                HttpContext.Session.SetObject("Carrito", carritoSesion);
                ViewBag.Alerta = "⚠️ Tu carrito se actualizó porque el inventario cambió.";
            }

            return View(carritoSesion);
        }

        // POST: Agregar al carrito
        // Acepta returnUrl para saber a dónde regresar
        // Acepta comprarAhora para saber si redireccionar al checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int id, int cantidad, string returnUrl = null, bool comprarAhora = false)
        {
            // 1. Validaciones básicas
            if (cantidad <= 0)
            {
                TempData["Error"] = "La cantidad debe ser mayor a 0.";
                return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito") ?? new List<ItemCarrito>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);

            // 2. Validar Stock
            int cantidadActual = item?.Cantidad ?? 0;
            if (cantidadActual + cantidad > producto.Stock)
            {
                TempData["Error"] = $"Stock insuficiente. Solo quedan {producto.Stock} piezas.";
                return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
            }

            // 3. Agregar o Actualizar
            if (item != null)
            {
                item.Cantidad += cantidad;
            }
            else
            {
                carrito.Add(new ItemCarrito
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.PrecioVenta,
                    Cantidad = cantidad,
                    StockMaximo = producto.Stock,
                    ImagenUrl = producto.ImagenUrl
                });
            }

            HttpContext.Session.SetObject("Carrito", carrito);

            // 4. LÓGICA DE RESPUESTA (AQUÍ ESTÁ EL CAMBIO)
            if (comprarAhora)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                // En lugar de un mensaje simple, enviamos datos para el SweetAlert "Rico"
                // Usamos una clave especial "AlertaCarrito" para que el Layout sepa qué mostrar
                TempData["AlertaCarrito"] = "true";
                TempData["ProductoAgregado"] = producto.Nombre;
                TempData["CantidadAgregada"] = cantidad;

                // IMPORTANTE: Regresamos EXACTAMENTE a donde estábamos (Detalles o Home)
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult Eliminar(int id)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");
            if (carrito != null)
            {
                var item = carrito.FirstOrDefault(c => c.ProductoId == id);
                if (item != null)
                {
                    carrito.Remove(item);
                    HttpContext.Session.SetObject("Carrito", carrito);
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}