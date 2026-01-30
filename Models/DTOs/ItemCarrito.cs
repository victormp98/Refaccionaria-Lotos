namespace RefaccionariaWeb.Models.DTOs
{
    public class ItemCarrito
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public int StockMaximo { get; set; } // Stock Real (Foto del momento)
        public string ImagenUrl { get; set; }
        public bool EsValido { get; set; } = true; // Para marcar en ROJO si ya no hay stock
        public string MensajeError { get; set; } // "Solo quedan 2", "Agotado", etc.

        public decimal SubTotal => Precio * Cantidad;
    }
}