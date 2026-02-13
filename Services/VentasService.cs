using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefaccionariaWeb.Data;
using RefaccionariaWeb.Models;
using RefaccionariaWeb.Models.DTOs;
using RefaccionariaWeb.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Services
{
    public class VentasService : IVentasService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAlmacenService _almacenService; // Para operaciones de stock
        private readonly UserManager<IdentityUser> _userManager; // Para obtener el usuario Público General

        public VentasService(ApplicationDbContext context, IAlmacenService almacenService, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _almacenService = almacenService;
            _userManager = userManager;
        }

        public async Task<int?> ProcesarVentaMostrador(List<ItemCarrito> carrito, string empleadoId, int corteCajaId, string nombreReceptor, string metodoPago, bool aplicaIVA, string? rfc = null, string? razonSocial = null)
        {
            if (carrito == null || !carrito.Any()) return null;

            // Validar que el empleado existe y el corte de caja es válido (lógica que podría venir del controller o de un CortesService)
            // Por ahora, asumimos que corteCajaId y empleadoId son válidos.
            if (string.IsNullOrEmpty(empleadoId)) throw new ArgumentNullException(nameof(empleadoId));
            if (corteCajaId <= 0) throw new ArgumentOutOfRangeException(nameof(corteCajaId));
            
            // Asegurar que el usuario "Público General" existe y obtener su ID
            string publicoGeneralId = DbInitializer.PublicoGeneralUserId;
            if (string.IsNullOrEmpty(publicoGeneralId))
            {
                 // Si por alguna razón el ID no se guardó o el usuario no existe, se busca
                 var publicUser = await _userManager.FindByEmailAsync(DbInitializer.PUBLICO_GENERAL_EMAIL);
                 if(publicUser == null) throw new InvalidOperationException("Usuario 'Público General' no encontrado o no inicializado.");
                 publicoGeneralId = publicUser.Id;
            }

            // Iniciar transacción de base de datos para asegurar atomicidad
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal subtotal = carrito.Sum(x => x.Cantidad * x.Precio);
                decimal totalFinal = aplicaIVA ? subtotal * 1.16m : subtotal;
                string infoIva = aplicaIVA ? " (Con IVA 16%)" : " (Sin IVA)";

                var pedido = new Pedido
                {
                    ClienteId = publicoGeneralId, // Cliente siempre será el "Público General" para mostrador
                    EmpleadoId = empleadoId,     // El empleado que realiza la venta
                    FechaPedido = DateTime.Now,
                    Status = PedidoStatus.Entregado, // Venta de mostrador se considera entregada de inmediato
                    TotalPedido = totalFinal,
                    NombreReceptor = nombreReceptor ?? "Público General",
                    DireccionEnvio = $"Mostrador - {metodoPago ?? "Efectivo"}{infoIva}", // Se usa para descripción
                    CiudadEnvio = null,      // Ahora nullable
                    EstadoEnvio = null,      // Ahora nullable
                    CodigoPostalEnvio = null, // Ahora nullable
                    PaisEnvio = "México",
                    TipoEntrega = 2,         // Venta Mostrador
                    RequiereFactura = !string.IsNullOrEmpty(rfc),
                    Rfc = rfc,
                    RazonSocial = razonSocial,
                    CorteCajaId = corteCajaId
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync(); // Guarda el pedido para obtener su Id

                foreach (var item in carrito)
                {
                    // Obtener producto para verificar stock y detalles
                    var producto = await _almacenService.ObtenerProductoPorId(item.ProductoId);
                    
                    if (producto == null || producto.Eliminado)
                        throw new Exception($"El producto '{item.Nombre}' ya no existe o está eliminado.");

                    if (producto.Stock < item.Cantidad)
                        throw new Exception($"¡Venta cancelada! Stock insuficiente para '{item.Nombre}'. Solo quedan {producto.Stock} unidades.");

                    // Descontar stock usando el AlmacenService
                    var stockActualizado = await _almacenService.ActualizarStock(
                        productoId: item.ProductoId,
                        cantidad: -item.Cantidad, // Cantidad negativa para descontar
                        motivo: "Salida Venta Mostrador",
                        usuarioId: empleadoId // El empleado que descontó el stock
                    );

                    if (!stockActualizado)
                        throw new Exception($"Error al actualizar stock para '{item.Nombre}'.");

                    // Crear Detalle de Pedido
                    _context.DetallesPedido.Add(new DetallePedido
                    {
                        PedidoId = pedido.Id,
                        ProductoId = item.ProductoId,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Precio
                    });
                }

                await _context.SaveChangesAsync(); // Guarda los detalles del pedido y movimientos de inventario del servicio
                await transaction.CommitAsync();

                return pedido.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Aquí podrías loggear el error
                throw new InvalidOperationException("Error al procesar la venta de mostrador: " + ex.Message, ex);
            }
        }
    }
}
