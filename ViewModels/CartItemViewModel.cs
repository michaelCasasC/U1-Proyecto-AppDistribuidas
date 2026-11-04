using Proyecto.Models;

namespace Proyecto.ViewModels
{
    public class CartItemViewModel
    {
        public Producto Producto { get; set; } = null!;
        public int Cantidad { get; set; }
        public decimal Subtotal => Producto.Precio * Cantidad;
    }
}
