using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models
{
    public class Emprendimiento
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        [StringLength(2000)]
        public string? ImagenUrl { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime? FechaEliminacion { get; set; }

        [StringLength(100)]
        public string? EliminadoPor { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Required]
        public int UsuarioId { get; set; }
        public ApplicationUser? Usuario { get; set; }

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}