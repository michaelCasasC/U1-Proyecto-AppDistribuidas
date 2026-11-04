using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto.Models
{
    public class Pago
    {
        // Estados posibles del pago
        public const string EstadoCreado = "CREATED";
        public const string EstadoCompletado = "COMPLETED";
        public const string EstadoAprobado = "APPROVED";
        public const string EstadoPagado = "PAID";
        public const string EstadoFallido = "FAILED";
        public const string EstadoError = "ERROR";
        public const string EstadoCancelado = "CANCELLED";
        public const string EstadoExpirado = "EXPIRED";
        public const string EstadoReembolsado = "REFUNDED";

        public int Id { get; set; }

        [Required]
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        [StringLength(100)]
        public string? PayPalOrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = EstadoCreado;

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Monto { get; set; }

        [StringLength(10)]
        public string Moneda { get; set; } = "USD";

        [StringLength(50)]
        public string MetodoPago { get; set; } = "PayPal";

        [StringLength(200)]
        public string? TransaccionId { get; set; }

        [StringLength(500)]
        public string? PayPalPayerId { get; set; }

        [StringLength(1000)]
        public string? PayPalPaymentId { get; set; }

        [StringLength(2000)]
        public string? DetallesJson { get; set; }

        [StringLength(500)]
        public string? DetalleRespuesta { get; set; }

        // PayPhone fields
        [StringLength(500)]
        public string? PayPhonePaymentUrl { get; set; }

        [StringLength(200)]
        public string? PayPhoneTransactionId { get; set; }

        [StringLength(200)]
        public string? PayPhoneClientTransactionId { get; set; }

        public int IntentosVerificacion { get; set; }

        public DateTime? UltimoIntentoVerificacion { get; set; }

        public DateTime? FechaConfirmacion { get; set; }

        [StringLength(1000)]
        public string? MensajeRespuesta { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }
    }
}
