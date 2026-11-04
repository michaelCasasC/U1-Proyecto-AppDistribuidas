namespace Proyecto.ViewModels.Reports;

public class PagosPorEstadoViewModel
{
    public List<PagosPorEstadoItem> Items { get; set; } = new();
}

public class PagosPorEstadoItem
{
    public string Estado { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal MontoTotal { get; set; }
}
