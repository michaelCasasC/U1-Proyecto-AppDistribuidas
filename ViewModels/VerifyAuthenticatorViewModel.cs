using System.ComponentModel.DataAnnotations;

namespace Proyecto.ViewModels
{
    public class VerifyAuthenticatorViewModel
    {
        [Required(ErrorMessage = "Ingresa el código de verificación.")]
        [StringLength(7, ErrorMessage = "El código debe tener 6 dígitos.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Código de autenticación")]
        public string Code { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
