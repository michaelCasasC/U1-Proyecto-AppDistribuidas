namespace Proyecto.Services;

public interface INotificacionService
{
    Task StockActualizadoAsync(int productoId, string nombre, int stockNuevo, CancellationToken cancellationToken = default);
    Task NuevoPedidoAsync(int pedidoId, decimal total, IEnumerable<int> emprendimientoIds, CancellationToken cancellationToken = default);
    Task EstadoPedidoCambiadoAsync(int pedidoId, string estadoNuevo, int usuarioId, CancellationToken cancellationToken = default);
}
