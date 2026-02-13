using RefaccionariaWeb.Models;
using RefaccionariaWeb.Models.DTOs; // Para ItemCarrito
using RefaccionariaWeb.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RefaccionariaWeb.Services
{
    public interface IVentasService
    {
        /// <summary>
        /// Procesa una venta realizada en el mostrador.
        /// Asigna el Pedido al usuario "Público General" y registra el EmpleadoId.
        /// </summary>
        /// <param name="carrito">Lista de productos en el carrito.</param>
        /// <param name="empleadoId">ID del empleado que realiza la venta.</param>
        /// <param name="corteCajaId">ID del corte de caja actual.</param>
        /// <param name="nombreReceptor">Nombre del cliente (puede ser "Público General").</param>
        /// <param name="metodoPago">Método de pago (Ej: "Efectivo", "Tarjeta").</param>
        /// <param name="aplicaIVA">Indica si se aplica IVA.</param>
        /// <param name="rfc">RFC del cliente para facturación (opcional).</param>
        /// <param name="razonSocial">Razón Social del cliente para facturación (opcional).</param>
        /// <returns>El ID del pedido creado si la venta es exitosa, null en caso contrario.</returns>
        Task<int?> ProcesarVentaMostrador(List<ItemCarrito> carrito, string empleadoId, int corteCajaId, string nombreReceptor, string metodoPago, bool aplicaIVA, string? rfc = null, string? razonSocial = null);

        // Otros métodos relacionados con la gestión general de pedidos (lectura, actualización de estado, etc.)
        // se añadirían aquí o en un servicio más general de Pedidos si la separación lo amerita.
    }
}
