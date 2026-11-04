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
    public class CategoriasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserSessionService _userSession;

        public CategoriasController(AppDbContext context, IUserSessionService userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        // GET: Categorias
        public async Task<IActionResult> Index()
        {
            var categorias = _context.Categorias.AsQueryable();
            if (_userSession.IsAdmin)
            {
                categorias = categorias.IgnoreQueryFilters();
            }
            else
            {
                categorias = categorias.Where(c => c.Activo);
            }

            return View(await categorias.ToListAsync());
        }

        // GET: Categorias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(m => m.Id == id);
            if (categoria == null || (!_userSession.IsAdmin && !categoria.Activo))
            {
                return NotFound();
            }

            return View(categoria);
        }

        // GET: Categorias/Create
        public IActionResult Create()
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            return View();
        }

        // POST: Categorias/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Descripcion,Activo,FechaCreacion")] Categoria categoria)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (ModelState.IsValid)
            {
                _context.Add(categoria);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }

        // GET: Categorias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }
            return View(categoria);
        }

        // POST: Categorias/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Activo,FechaCreacion")] Categoria categoria)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id != categoria.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(categoria);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoriaExists(categoria.Id))
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
            return View(categoria);
        }

        // GET: Categorias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(m => m.Id == id);
            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // POST: Categorias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria != null)
            {
                categoria.Activo = false;
                categoria.FechaEliminacion = DateTime.UtcNow;
                categoria.EliminadoPor = User.Identity?.Name;
                var productos = await _context.Productos.Where(p => p.CategoriaId == categoria.Id).ToListAsync();
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

        private bool CategoriaExists(int id)
        {
            return _context.Categorias.Any(e => e.Id == id);
        }

        private IActionResult? EnsureAdmin()
        {
            if (!_userSession.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            return _userSession.IsAdmin ? null : RedirectToAction("AccessDenied", "Home");
        }
    }
}
