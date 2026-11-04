using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Models;
using Proyecto.Services;

namespace Proyecto.Controllers
{
    public class EmprendimientosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserSessionService _userSession;

        public EmprendimientosController(AppDbContext context, IUserSessionService userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        // GET: Emprendimientos
        public async Task<IActionResult> Index()
        {
            var emprendimientos = _context.Emprendimientos.Include(e => e.Usuario).AsQueryable();

            if (_userSession.IsAdmin)
            {
                emprendimientos = emprendimientos.IgnoreQueryFilters();
            }
            else if (_userSession.IsEntrepreneur)
            {
                var currentUserId = _userSession.GetCurrentUserId();
                emprendimientos = emprendimientos.Where(e => e.UsuarioId == currentUserId);
            }
            else
            {
                emprendimientos = emprendimientos.Where(e => e.Activo);
            }

            return View(await emprendimientos.ToListAsync());
        }

        // GET: Emprendimientos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var emprendimiento = await _context.Emprendimientos
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (emprendimiento == null || !CanView(emprendimiento))
            {
                return NotFound();
            }

            return View(emprendimiento);
        }

        // GET: Emprendimientos/Create
        public IActionResult Create()
        {
            if (!_userSession.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Emprendimientos") });
            }

            if (!_userSession.IsAdmin && !_userSession.IsEntrepreneur)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View();
        }

        // POST: Emprendimientos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Descripcion,Activo,FechaCreacion")] Emprendimiento emprendimiento)
        {
            if (!_userSession.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Emprendimientos") });
            }

            if (!_userSession.IsAdmin && !_userSession.IsEntrepreneur)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var currentUserId = _userSession.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Emprendimientos") });
            }

            if (ModelState.IsValid)
            {
                emprendimiento.UsuarioId = _userSession.IsAdmin && emprendimiento.UsuarioId > 0
                    ? emprendimiento.UsuarioId
                    : currentUserId.Value;
                emprendimiento.FechaCreacion = DateTime.UtcNow;
                emprendimiento.Activo = true;

                _context.Add(emprendimiento);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(emprendimiento);
        }

        // GET: Emprendimientos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var emprendimiento = await _context.Emprendimientos.FindAsync(id);
            if (emprendimiento == null || !CanManage(emprendimiento))
            {
                return NotFound();
            }
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Email", emprendimiento.UsuarioId);
            return View(emprendimiento);
        }

        // POST: Emprendimientos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Activo,FechaCreacion,UsuarioId")] Emprendimiento emprendimiento)
        {
            if (id != emprendimiento.Id)
            {
                return NotFound();
            }

            var existing = await _context.Emprendimientos.FindAsync(id);
            if (existing == null || !CanManage(existing))
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existing.Nombre = emprendimiento.Nombre;
                    existing.Descripcion = emprendimiento.Descripcion;
                    existing.Activo = emprendimiento.Activo;
                    existing.FechaCreacion = emprendimiento.FechaCreacion;
                    if (_userSession.IsAdmin)
                    {
                        existing.UsuarioId = emprendimiento.UsuarioId;
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmprendimientoExists(emprendimiento.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Email", emprendimiento.UsuarioId);
            return View(emprendimiento);
        }

        // GET: Emprendimientos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var emprendimiento = await _context.Emprendimientos
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (emprendimiento == null || !CanManage(emprendimiento))
            {
                return NotFound();
            }

            return View(emprendimiento);
        }

        // POST: Emprendimientos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var emprendimiento = await _context.Emprendimientos.FindAsync(id);
            if (emprendimiento != null && CanManage(emprendimiento))
            {
                emprendimiento.Activo = false;
                emprendimiento.FechaEliminacion = DateTime.UtcNow;
                emprendimiento.EliminadoPor = User.Identity?.Name;
                var productos = await _context.Productos
                    .Where(p => p.EmprendimientoId == emprendimiento.Id)
                    .ToListAsync();
                foreach (var producto in productos)
                {
                    producto.Activo = false;
                    producto.FechaEliminacion = DateTime.UtcNow;
                    producto.EliminadoPor = User.Identity?.Name;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmprendimientoExists(int id)
        {
            return _context.Emprendimientos.Any(e => e.Id == id);
        }

        private bool CanView(Emprendimiento emprendimiento)
        {
            if (_userSession.IsAdmin) return true;
            if (_userSession.IsEntrepreneur) return emprendimiento.UsuarioId == _userSession.GetCurrentUserId();
            return emprendimiento.Activo;
        }

        private bool CanManage(Emprendimiento emprendimiento)
        {
            if (_userSession.IsAdmin) return true;
            return _userSession.IsEntrepreneur && emprendimiento.UsuarioId == _userSession.GetCurrentUserId();
        }
    }
}
