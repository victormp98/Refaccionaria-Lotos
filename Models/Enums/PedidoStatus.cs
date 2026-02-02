namespace RefaccionariaWeb.Models.Enums
{
    public enum PedidoStatus
    {
        // El pedido ha sido creado pero aún no ha sido pagado/confirmado
        PendienteDePago = 0,
        // El pago ha sido recibido y el pedido está listo para ser procesado
        Pagado = 1,
        // El pedido está siendo preparado por el personal de almacén
        EnProceso = 2,
        // El pedido ha sido enviado al cliente
        Enviado = 3,
        // El pedido ha sido recibido por el cliente
        Entregado = 4,
        // El pedido ha sido cancelado (por el cliente o por el administrador)
        Cancelado = 5
    }
}
