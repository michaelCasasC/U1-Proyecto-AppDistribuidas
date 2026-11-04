using Proyecto.Models;
using Proyecto.ViewModels;

namespace Proyecto.Services;

public interface IAccountService
{
    Task<ApplicationUser?> AuthenticateAsync(string email, string password);
    Task<ApplicationUser> RegisterAsync(RegisterViewModel model);
    Task<bool> EmailExistsAsync(string email);
    Task<(ApplicationUser user, bool fueCreado)> FindOrCreateExternalUserAsync(string email, string nombre);
    Task<ApplicationUser?> GetByIdAsync(int id);
}
