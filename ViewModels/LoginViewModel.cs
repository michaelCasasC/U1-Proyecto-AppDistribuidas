using System.ComponentModel.DataAnnotations;

namespace Proyecto.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recuérdame")]
        public bool RememberMe { get; set; }
    }
}
