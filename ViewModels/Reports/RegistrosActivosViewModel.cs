namespace Proyecto.ViewModels.Reports;

public class RegistrosActivosViewModel
{
    public List<RegistrosActivosItem> Items { get; set; } = new();
}

public class RegistrosActivosItem
{
    public string Entidad { get; set; } = "";
    public int Activos { get; set; }
    public int Inactivos { get; set; }
    public int Total { get; set; }
    public double PorcentajeActivos { get; set; }
}
