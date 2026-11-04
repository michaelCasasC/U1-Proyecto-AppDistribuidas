using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserSessionService _userSession;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsuariosController(
            AppDbContext context,
            IUserSessionService userSession,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userSession = userSession;
            _userManager = userManager;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            return View(await _userManager.Users.IgnoreQueryFilters().ToListAsync());
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id == null) return NotFound();

            var usuario = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre,Email,Telefono,EsCliente,EsEmprendedor,EsAdministrador,Activo,FechaCreacion")] ApplicationUser usuario,
            string password = "Temporal123!")
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (ModelState.IsValid)
            {
                usuario.UserName = usuario.Email;
                usuario.EmailConfirmed = true;
                var result = await _userManager.CreateAsync(usuario, password);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(usuario);
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id == null) return NotFound();

            var usuario = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Nombre,Email,Telefono,EsCliente,EsEmprendedor,EsAdministrador,Activo,FechaCreacion")] ApplicationUser usuario)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id != usuario.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var usuarioExistente = await _userManager.FindByIdAsync(id.ToString());
                if (usuarioExistente == null) return NotFound();

                usuarioExistente.Nombre = usuario.Nombre;
                usuarioExistente.Email = usuario.Email;
                usuarioExistente.UserName = usuario.Email;
                usuarioExistente.Telefono = usuario.Telefono;
                usuarioExistente.EsCliente = usuario.EsCliente;
                usuarioExistente.EsEmprendedor = usuario.EsEmprendedor;
                usuarioExistente.EsAdministrador = usuario.EsAdministrador;
                usuarioExistente.Activo = usuario.Activo;
                usuarioExistente.FechaCreacion = usuario.FechaCreacion;

                var result = await _userManager.UpdateAsync(usuarioExistente);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(usuario);
        }

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id == null) return NotFound();

            var usuario = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            var usuario = await _userManager.FindByIdAsync(id.ToString());
            if (usuario != null)
            {
                // Soft delete: no se borra el registro, solo se desactiva
                usuario.Activo = false;
                usuario.FechaEliminacion = DateTime.UtcNow;
                usuario.EliminadoPor = User.Identity?.Name;
                await _userManager.UpdateAsync(usuario);
            }

            return RedirectToAction(nameof(Index));
        }

        private IActionResult? EnsureAdmin()
        {
            if (!_userSession.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account",
                    new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            if (!_userSession.IsAdmin)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return null;
        }
    }
}
