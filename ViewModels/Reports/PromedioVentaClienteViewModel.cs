namespace Proyecto.ViewModels.Reports;

public class PromedioVentaClienteViewModel
{
    public decimal PromedioGlobal { get; set; }
    public int TotalClientesConCompras { get; set; }
    public int TotalPedidos { get; set; }
    public decimal IngresoTotal { get; set; }
    public List<PromedioVentaClienteItem> Items { get; set; } = new();
}

public class PromedioVentaClienteItem
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public int CantidadCompras { get; set; }
    public decimal TotalGastado { get; set; }
    public decimal Promedio { get; set; }
}
