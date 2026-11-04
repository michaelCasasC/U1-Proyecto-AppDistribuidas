using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models
{
    public class DetallePedido
    {
        public int Id { get; set; }

        [Required]
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal PrecioUnitario { get; set; }

        [NotMapped]
        public decimal Total => Cantidad * PrecioUnitario;

        public bool Activo { get; set; } = true;

        public DateTime? FechaEliminacion { get; set; }

        [StringLength(100)]
        public string? EliminadoPor { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}