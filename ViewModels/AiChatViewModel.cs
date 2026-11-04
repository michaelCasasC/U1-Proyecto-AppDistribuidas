using System.ComponentModel.DataAnnotations;

namespace Proyecto.ViewModels
{
    public class AiChatViewModel
    {
        [Required(ErrorMessage = "Escribe una pregunta para el asistente.")]
        [StringLength(500, ErrorMessage = "La pregunta no puede superar los 500 caracteres.")]
        [Display(Name = "Pregunta")]
        public string Question { get; set; } = string.Empty;

        public string? Answer { get; set; }

        public string? ErrorMessage { get; set; }

        public List<AiProductContextItem> AvailableProducts { get; set; } = new();
    }

    public class AiProductContextItem
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public decimal Precio { get; set; }

        public int Stock { get; set; }

        public string Categoria { get; set; } = string.Empty;

        public string Emprendimiento { get; set; } = string.Empty;
    }
}
