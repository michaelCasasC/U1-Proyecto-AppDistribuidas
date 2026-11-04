using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime? FechaEliminacion { get; set; }

        [StringLength(100)]
        public string? EliminadoPor { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}