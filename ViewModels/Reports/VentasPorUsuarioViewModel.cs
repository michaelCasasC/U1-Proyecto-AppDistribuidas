namespace Proyecto.ViewModels.Reports;

public class VentasPorUsuarioViewModel
{
    public List<VentasPorUsuarioItem> Items { get; set; } = new();
}

public class VentasPorUsuarioItem
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public int CantidadPedidos { get; set; }
    public decimal TotalGastado { get; set; }
}
