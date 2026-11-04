namespace Proyecto.ViewModels.Reports;

public class ProductosMasVendidosViewModel
{
    public List<ProductosMasVendidosItem> Items { get; set; } = new();
}

public class ProductosMasVendidosItem
{
    public string Producto { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Emprendimiento { get; set; } = "";
    public int CantidadVendida { get; set; }
    public decimal TotalGenerado { get; set; }
}
