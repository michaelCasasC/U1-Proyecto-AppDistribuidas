using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models;

public class MovimientoInventario
{
    public int Id { get; set; }

    [Required]
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int? PedidoId { get; set; }

    [Required]
    [StringLength(20)]
    public string Tipo { get; set; } = string.Empty;

    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }

    [StringLength(500)]
    public string? Motivo { get; set; }

    public int? UsuarioId { get; set; }

    [StringLength(50)]
    public string? ReferenciaExterna { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
