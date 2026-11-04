using Proyecto.Models;

namespace Proyecto.ViewModels
{
    public class PedidoIndexViewModel
    {
        public List<Pedido> Pedidos { get; set; } = new();

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public int TotalRecords { get; set; }

        public int PageSize { get; set; }
    }
}