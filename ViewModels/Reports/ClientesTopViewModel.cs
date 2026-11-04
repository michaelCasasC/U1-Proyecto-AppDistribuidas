namespace Proyecto.ViewModels.Reports;

public class ClientesTopViewModel
{
    public List<ClientesTopItem> Items { get; set; } = new();
}

public class ClientesTopItem
{
    public int UsuarioId { get; set; }
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public int CantidadCompras { get; set; }
    public decimal TotalGastado { get; set; }
}
