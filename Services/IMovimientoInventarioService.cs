namespace Proyecto.Services;

public interface IMovimientoInventarioService
{
    Task RegistrarAsync(int productoId, int cantidad, string tipo, int stockAnterior, int stockNuevo,
        string? motivo = null, int? pedidoId = null, int? usuarioId = null, string? referenciaExterna = null);
    Task<List<MovimientoInventarioEntry>> ObtenerPorProductoAsync(int productoId, int count = 50);
    Task<List<MovimientoInventarioEntry>> ObtenerRecientesAsync(int count = 100);
}

public class MovimientoInventarioEntry
{
    public int Id { get; set; }
    public string Producto { get; set; } = "";
    public int ProductoId { get; set; }
    public int? PedidoId { get; set; }
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public string? Motivo { get; set; }
    public DateTime Fecha { get; set; }
}
