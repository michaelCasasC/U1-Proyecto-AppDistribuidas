using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Models;

namespace Proyecto.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;
    private readonly IMovimientoInventarioService _movimientoService;
    private readonly INotificacionService _notificacionService;

    public InventoryService(
        AppDbContext context,
        IMovimientoInventarioService movimientoService,
        INotificacionService notificacionService)
    {
        _context = context;
        _movimientoService = movimientoService;
        _notificacionService = notificacionService;
    }

    public async Task<bool> DeductStockAsync(int pedidoId, int? usuarioId = null)
    {
        var detalles = await _context.DetallesPedido
            .Include(d => d.Producto)
            .Where(d => d.PedidoId == pedidoId)
            .ToListAsync();

        foreach (var detalle in detalles)
        {
            if (detalle.Producto != null)
            {
                var stockAnterior = detalle.Producto.Stock;
                detalle.Producto.Stock -= detalle.Cantidad;

                await _movimientoService.RegistrarAsync(
                    detalle.ProductoId,
                    detalle.Cantidad,
                    "SALIDA",
                    stockAnterior,
                    detalle.Producto.Stock,
                    motivo: $"Venta aprobada - Pedido #{pedidoId}",
                    pedidoId: pedidoId,
                    usuarioId: usuarioId);
            }
        }

        await _context.SaveChangesAsync();
        foreach (var detalle in detalles.Where(d => d.Producto != null))
        {
            await _notificacionService.StockActualizadoAsync(
                detalle.ProductoId,
                detalle.Producto!.Nombre,
                detalle.Producto.Stock);
        }
        return true;
    }

    public async Task<bool> RestoreStockAsync(int pedidoId, int? usuarioId = null)
    {
        var detalles = await _context.DetallesPedido
            .Include(d => d.Producto)
            .Where(d => d.PedidoId == pedidoId)
            .ToListAsync();

        foreach (var detalle in detalles)
        {
            if (detalle.Producto != null)
            {
                var stockAnterior = detalle.Producto.Stock;
                detalle.Producto.Stock += detalle.Cantidad;

                await _movimientoService.RegistrarAsync(
                    detalle.ProductoId,
                    detalle.Cantidad,
                    "ENTRADA",
                    stockAnterior,
                    detalle.Producto.Stock,
                    motivo: $"Devolución por pago fallido/cancelado - Pedido #{pedidoId}",
                    pedidoId: pedidoId,
                    usuarioId: usuarioId);
            }
        }

        await _context.SaveChangesAsync();
        foreach (var detalle in detalles.Where(d => d.Producto != null))
        {
            await _notificacionService.StockActualizadoAsync(
                detalle.ProductoId,
                detalle.Producto!.Nombre,
                detalle.Producto.Stock);
        }
        return true;
    }

    public async Task<List<LowStockProduct>> GetLowStockProductsAsync(int minStock = 10)
    {
        return await _context.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Emprendimiento)
            .Where(p => p.Activo && p.Stock <= minStock)
            .Select(p => new LowStockProduct
            {
                ProductoId = p.Id,
                Nombre = p.Nombre,
                Stock = p.Stock,
                Categoria = p.Categoria!.Nombre,
                Emprendimiento = p.Emprendimiento!.Nombre
            })
            .OrderBy(p => p.Stock)
            .ToListAsync();
    }

    public async Task<int> GetStockAsync(int productoId)
    {
        var producto = await _context.Productos.FindAsync(productoId);
        return producto?.Stock ?? 0;
    }
}
