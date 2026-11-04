using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.ViewModels;

namespace Proyecto.Services
{
    /// <summary>
    /// Servicio de usuario que delega en UserManager de ASP.NET Core Identity.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UserService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<ApplicationUser?> AuthenticateAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.Activo)
                return null;

            // CheckPasswordSignInAsync valida la contraseña sin crear cookie de Identity.
            // La sesión la sigue manejando IUserSessionService (sesión en memoria).
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
            return result.Succeeded ? user : null;
        }

        public async Task<ApplicationUser> CreateUserAsync(RegisterViewModel model)
        {
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
                throw new InvalidOperationException("El correo electrónico ya está registrado.");

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,          // sin flujo de confirmación por email
                Nombre = model.Nombre,
                Telefono = model.Telefono,
                EsCliente = model.EsCliente,
                EsEmprendedor = model.EsEmprendedor,
                EsAdministrador = false,        // los admins solo se crean por seed o BBDD
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Error al crear el usuario: {errors}");
            }

            // Asignar roles según flags
            if (user.EsCliente)
                await _userManager.AddToRoleAsync(user, "Cliente");
            if (user.EsEmprendedor)
                await _userManager.AddToRoleAsync(user, "Emprendedor");

            return user;
        }

        public async Task<ApplicationUser?> GetByIdAsync(int id)
        {
            return await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        public async Task<(ApplicationUser user, bool fueCreado)> FindOrCreateExternalUserAsync(string email, string nombre)
        {
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
                return (existing, false);

            // Usuario nuevo vía Google: se crea como Cliente activo.
            // Sin contraseña (login solo externo) y EmailConfirmed=FALSE hasta
            // que verifique el correo mediante el token que le enviamos.
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                Nombre = nombre,
                EsCliente = true,
                EsEmprendedor = false,
                EsAdministrador = false,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Error al crear usuario externo: {errors}");
            }

            await _userManager.AddToRoleAsync(user, "Cliente");
            return (user, true);
        }
    }
}
