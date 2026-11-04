using System.ComponentModel.DataAnnotations;

namespace Proyecto.ViewModels
{
    public class EnableAuthenticatorViewModel
    {
        public string SharedKey { get; set; } = string.Empty;

        public string AuthenticatorUri { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el código de verificación.")]
        [StringLength(7, ErrorMessage = "El código debe tener 6 dígitos.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Código de verificación")]
        public string Code { get; set; } = string.Empty;
    }
}
