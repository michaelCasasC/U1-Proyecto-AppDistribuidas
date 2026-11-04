using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto.Models;
using Proyecto.ViewModels;

namespace Proyecto.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountService(
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

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        return result.Succeeded ? user : null;
    }

    public async Task<ApplicationUser> RegisterAsync(RegisterViewModel model)
    {
        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
            throw new InvalidOperationException("El correo electrónico ya está registrado.");

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            Nombre = model.Nombre,
            Telefono = model.Telefono,
            EsCliente = model.EsCliente,
            EsEmprendedor = model.EsEmprendedor,
            EsAdministrador = false,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Error al crear el usuario: {errors}");
        }

        if (user.EsCliente)
            await _userManager.AddToRoleAsync(user, "Cliente");
        if (user.EsEmprendedor)
            await _userManager.AddToRoleAsync(user, "Emprendedor");

        return user;
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

    public async Task<ApplicationUser?> GetByIdAsync(int id)
    {
        return await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
    }
}
