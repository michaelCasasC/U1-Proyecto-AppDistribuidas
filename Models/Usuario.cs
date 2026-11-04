using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Telefono { get; set; }

        [ScaffoldColumn(false)]
        [StringLength(500)]
        public string? PasswordHash { get; set; }

        [Display(Name = "Cliente")]
        public bool EsCliente { get; set; } = true;

        [Display(Name = "Emprendedor")]
        public bool EsEmprendedor { get; set; } = false;

        [Display(Name = "Administrador")]
        public bool EsAdministrador { get; set; } = false;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public ICollection<Emprendimiento> Emprendimientos { get; set; } = new List<Emprendimiento>();
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
