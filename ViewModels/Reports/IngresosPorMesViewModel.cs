namespace Proyecto.ViewModels.Reports;

public class IngresosPorMesViewModel
{
    public List<IngresosPorMesItem> Items { get; set; } = new();
    public string ChartLabels { get; set; } = "";
    public string ChartIngresos { get; set; } = "";
    public string ChartPedidos { get; set; } = "";
}

public class IngresosPorMesItem
{
    public int Anio { get; set; }
    public int Mes { get; set; }
    public string MesNombre { get; set; } = "";
    public int CantidadPedidos { get; set; }
    public decimal TotalIngresos { get; set; }
}
