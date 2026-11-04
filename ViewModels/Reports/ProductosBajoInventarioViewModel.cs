namespace Proyecto.ViewModels.Reports;

public class ProductosBajoInventarioViewModel
{
    public int StockMinimo { get; set; } = 10;
    public List<ProductosBajoInventarioItem> Items { get; set; } = new();
}

public class ProductosBajoInventarioItem
{
    public int ProductoId { get; set; }
    public string Producto { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Emprendimiento { get; set; } = "";
    public int Stock { get; set; }
    public decimal Precio { get; set; }
}
