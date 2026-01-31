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
                    item.Cantidad = productoReal.Stock; // Ajustamos al máximo disponible
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(int id, int cantidad, string returnUrl = null, bool comprarAhora = false)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito") ?? new List<ItemCarrito>();
            var item = carrito.FirstOrDefault(c => c.ProductoId == id);

            int cantidadEnCarrito = item?.Cantidad ?? 0;
            int totalDeseado = cantidadEnCarrito + cantidad;

            // LÓGICA DE AVISO DE STOCK
            if (totalDeseado > producto.Stock)
            {
                int capacidadLibre = producto.Stock - cantidadEnCarrito;

                if (capacidadLibre <= 0)
                {
                    TempData["Error"] = $"No puedes agregar más. Ya tienes las {producto.Stock} piezas disponibles en tu carrito.";
                }
                else
                {
                    // Agregamos solo lo que sobra para llegar al tope
                    if (item != null) item.Cantidad = producto.Stock;
                    else
                    {
                        carrito.Add(new ItemCarrito
                        {
                            ProductoId = producto.Id,
                            Nombre = producto.Nombre,
                            Precio = producto.PrecioVenta,
                            Cantidad = producto.Stock,
                            StockMaximo = producto.Stock,
                            ImagenUrl = producto.ImagenUrl
                        });
                    }
                    TempData["AlertaCarrito"] = "true";
                    TempData["ProductoAgregado"] = producto.Nombre;
                    TempData["CantidadAgregada"] = capacidadLibre;
                    TempData["Error"] = $"Solo se agregaron {capacidadLibre} piezas adicionales (Límite de stock alcanzado).";
                }
            }
            else
            {
                // Agregar normal
                if (item != null) item.Cantidad += cantidad;
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
                TempData["AlertaCarrito"] = "true";
                TempData["ProductoAgregado"] = producto.Nombre;
                TempData["CantidadAgregada"] = cantidad;
            }

            HttpContext.Session.SetObject("Carrito", carrito);
            return comprarAhora ? RedirectToAction(nameof(Index)) : (!string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home"));
        }

        public IActionResult ActualizarCantidad(int id, int cantidad)
        {
            var carrito = HttpContext.Session.GetObject<List<ItemCarrito>>("Carrito");
            if (carrito != null)
            {
                var item = carrito.FirstOrDefault(c => c.ProductoId == id);
                if (item != null && cantidad > 0 && cantidad <= item.StockMaximo)
                {
                    item.Cantidad = cantidad;
                    HttpContext.Session.SetObject("Carrito", carrito);
                }
            }
            return RedirectToAction(nameof(Index));
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