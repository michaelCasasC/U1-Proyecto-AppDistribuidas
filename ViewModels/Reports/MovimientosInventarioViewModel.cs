namespace Proyecto.ViewModels.Reports;

public class MovimientosInventarioViewModel
{
    public DateTime FechaInicio { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime FechaFin { get; set; } = DateTime.Today;
    public List<MovimientosInventarioItem> Items { get; set; } = new();
}

public class MovimientosInventarioItem
{
    public DateTime Fecha { get; set; }
    public string Producto { get; set; } = "";
    public string Emprendimiento { get; set; } = "";
    public int Cantidad { get; set; }
    public string Tipo { get; set; } = "Venta";
    public int? PedidoId { get; set; }
}
