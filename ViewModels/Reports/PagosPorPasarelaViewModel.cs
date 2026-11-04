namespace Proyecto.ViewModels.Reports;

public class PagosPorPasarelaViewModel
{
    public List<PagosPorPasarelaItem> Items { get; set; } = new();
}

public class PagosPorPasarelaItem
{
    public string MetodoPago { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal MontoTotal { get; set; }
    public int Aprobados { get; set; }
    public int Fallidos { get; set; }
}
