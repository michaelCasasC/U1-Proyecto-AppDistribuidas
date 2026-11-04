namespace Proyecto.ViewModels.Reports;

public class VentasPorFechaViewModel
{
    public DateTime FechaInicio { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime FechaFin { get; set; } = DateTime.Today;
    public List<VentasPorFechaItem> Items { get; set; } = new();
}

public class VentasPorFechaItem
{
    public DateTime Fecha { get; set; }
    public int CantidadPedidos { get; set; }
    public decimal TotalVentas { get; set; }
}
