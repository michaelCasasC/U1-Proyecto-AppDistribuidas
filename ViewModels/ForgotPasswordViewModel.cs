using System.ComponentModel.DataAnnotations;

namespace Proyecto.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;
}
