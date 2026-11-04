using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Models;

namespace Proyecto.Services;

public class MovimientoInventarioService : IMovimientoInventarioService
{
    private readonly AppDbContext _context;

    public MovimientoInventarioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(int productoId, int cantidad, string tipo, int stockAnterior, int stockNuevo,
        string? motivo = null, int? pedidoId = null, int? usuarioId = null, string? referenciaExterna = null)
    {
        var movimiento = new MovimientoInventario
        {
            ProductoId = productoId,
            PedidoId = pedidoId,
            Tipo = tipo,
            Cantidad = cantidad,
            StockAnterior = stockAnterior,
            StockNuevo = stockNuevo,
            Motivo = motivo,
            UsuarioId = usuarioId,
            ReferenciaExterna = referenciaExterna,
            Fecha = DateTime.UtcNow
        };

        _context.MovimientosInventario.Add(movimiento);
        await _context.SaveChangesAsync();
    }

    public async Task<List<MovimientoInventarioEntry>> ObtenerPorProductoAsync(int productoId, int count = 50)
    {
        return await _context.MovimientosInventario
            .Include(m => m.Producto)
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .Take(count)
            .Select(m => new MovimientoInventarioEntry
            {
                Id = m.Id,
                Producto = m.Producto!.Nombre,
                ProductoId = m.ProductoId,
                PedidoId = m.PedidoId,
                Tipo = m.Tipo,
                Cantidad = m.Cantidad,
                StockAnterior = m.StockAnterior,
                StockNuevo = m.StockNuevo,
                Motivo = m.Motivo,
                Fecha = m.Fecha
            })
            .ToListAsync();
    }

    public async Task<List<MovimientoInventarioEntry>> ObtenerRecientesAsync(int count = 100)
    {
        return await _context.MovimientosInventario
            .Include(m => m.Producto)
            .OrderByDescending(m => m.Fecha)
            .Take(count)
            .Select(m => new MovimientoInventarioEntry
            {
                Id = m.Id,
                Producto = m.Producto!.Nombre,
                ProductoId = m.ProductoId,
                PedidoId = m.PedidoId,
                Tipo = m.Tipo,
                Cantidad = m.Cantidad,
                StockAnterior = m.StockAnterior,
                StockNuevo = m.StockNuevo,
                Motivo = m.Motivo,
                Fecha = m.Fecha
            })
            .ToListAsync();
    }
}
