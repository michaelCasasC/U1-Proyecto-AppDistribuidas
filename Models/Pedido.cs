using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }
        public ApplicationUser? Usuario { get; set; }

        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Total { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime? FechaEliminacion { get; set; }

        [StringLength(100)]
        public string? EliminadoPor { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}