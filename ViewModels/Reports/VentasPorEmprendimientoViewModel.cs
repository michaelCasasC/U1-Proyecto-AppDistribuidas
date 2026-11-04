namespace Proyecto.ViewModels.Reports;

public class VentasPorEmprendimientoViewModel
{
    public List<VentasPorEmprendimientoItem> Items { get; set; } = new();
}

public class VentasPorEmprendimientoItem
{
    public string Emprendimiento { get; set; } = "";
    public string Usuario { get; set; } = "";
    public int CantidadVentas { get; set; }
    public decimal TotalVentas { get; set; }
}
