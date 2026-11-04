using Microsoft.AspNetCore.SignalR;
using Proyecto.Hubs;

namespace Proyecto.Services;

public sealed class NotificacionService : INotificacionService
{
    private readonly IHubContext<NotificacionesHub> _hub;

    public NotificacionService(IHubContext<NotificacionesHub> hub)
    {
        _hub = hub;
    }

    private static string Instancia => Environment.GetEnvironmentVariable("POD_NAME")
        ?? Environment.MachineName;

    public Task StockActualizadoAsync(int productoId, string nombre, int stockNuevo, CancellationToken cancellationToken = default) =>
        _hub.Clients.All.SendAsync(
            "StockActualizado",
            new { productoId, nombre, stockNuevo, instancia = Instancia },
            cancellationToken);

    public async Task NuevoPedidoAsync(int pedidoId, decimal total, IEnumerable<int> emprendimientoIds, CancellationToken cancellationToken = default)
    {
        var payload = new { pedidoId, total, instancia = Instancia };
        foreach (var emprendimientoId in emprendimientoIds.Distinct())
        {
            await _hub.Clients.Group($"emprendedor:{emprendimientoId}")
                .SendAsync("NuevoPedido", payload, cancellationToken);
        }
    }

    public Task EstadoPedidoCambiadoAsync(int pedidoId, string estadoNuevo, int usuarioId, CancellationToken cancellationToken = default) =>
        _hub.Clients.Group($"cliente:{usuarioId}").SendAsync(
            "EstadoPedidoCambiado",
            new { pedidoId, estadoNuevo, instancia = Instancia },
            cancellationToken);
}
