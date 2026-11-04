using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Accion { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Entidad { get; set; }

    public int? EntidadId { get; set; }

    public int? UsuarioId { get; set; }

    [StringLength(2000)]
    public string? Detalles { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
