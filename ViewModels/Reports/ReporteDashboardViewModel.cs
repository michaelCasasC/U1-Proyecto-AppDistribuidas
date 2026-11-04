namespace Proyecto.ViewModels.Reports;

public class ReporteDashboardViewModel
{
    public int TotalPedidos { get; set; }
    public int PedidosActivos { get; set; }
    public decimal IngresosTotales { get; set; }
    public int TotalClientes { get; set; }
    public int TotalEmprendimientos { get; set; }
    public int TotalProductos { get; set; }
    public int ProductosStockBajo { get; set; }
    public int TotalPagosExitosos { get; set; }
    public int TotalUsuarios { get; set; }
}
