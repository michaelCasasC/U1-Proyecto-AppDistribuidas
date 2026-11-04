using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models;

public class EmailLog
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Destinatario { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Asunto { get; set; } = string.Empty;

    public string? CuerpoHtml { get; set; }

    public bool Enviado { get; set; }

    [StringLength(500)]
    public string? Error { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
}
