using Microsoft.AspNetCore.SignalR;

namespace Proyecto.Hubs;

public sealed class NotificacionesHub : Hub
{
    public Task UnirseComoEmprendedor(int emprendimientoId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"emprendedor:{emprendimientoId}");

    public Task UnirseComoCliente(int usuarioId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"cliente:{usuarioId}");

    public Task SalirDeGrupo(string grupo) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo);
}
