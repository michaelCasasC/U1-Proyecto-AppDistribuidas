namespace Proyecto.Services;

public interface IInventoryService
{
    Task<bool> DeductStockAsync(int pedidoId, int? usuarioId = null);
    Task<bool> RestoreStockAsync(int pedidoId, int? usuarioId = null);
    Task<List<LowStockProduct>> GetLowStockProductsAsync(int minStock = 10);
    Task<int> GetStockAsync(int productoId);
}

public class LowStockProduct
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = "";
    public int Stock { get; set; }
    public string Categoria { get; set; } = "";
    public string Emprendimiento { get; set; } = "";
}
