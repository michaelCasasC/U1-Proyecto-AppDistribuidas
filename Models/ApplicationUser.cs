using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models
{
    /// <summary>
    /// Usuario de la aplicación basado en ASP.NET Core Identity.
    /// Extiende IdentityUser&lt;int&gt; para mantener la clave primaria como int
    /// y conservar las FK existentes en Pedidos y Emprendimientos sin cambios.
    /// </summary>
    public class ApplicationUser : IdentityUser<int>
    {
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Telefono { get; set; }

        [Display(Name = "Cliente")]
        public bool EsCliente { get; set; } = true;

        [Display(Name = "Emprendedor")]
        public bool EsEmprendedor { get; set; } = false;

        [Display(Name = "Administrador")]
        public bool EsAdministrador { get; set; } = false;

        public bool Activo { get; set; } = true;

        public DateTime? FechaEliminacion { get; set; }

        [StringLength(100)]
        public string? EliminadoPor { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Relaciones de navegación
        public ICollection<Emprendimiento> Emprendimientos { get; set; } = new List<Emprendimiento>();
        public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}
