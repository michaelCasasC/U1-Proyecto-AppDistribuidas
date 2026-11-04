using Proyecto.Models;
using Proyecto.ViewModels;

namespace Proyecto.Services
{
    public interface IUserService
    {
        Task<ApplicationUser?> AuthenticateAsync(string email, string password);
        Task<ApplicationUser> CreateUserAsync(RegisterViewModel model);
        Task<ApplicationUser?> GetByIdAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<(ApplicationUser user, bool fueCreado)> FindOrCreateExternalUserAsync(string email, string nombre);
    }
}
