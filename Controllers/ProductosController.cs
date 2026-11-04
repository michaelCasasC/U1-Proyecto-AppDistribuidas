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
    public class ProductosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserSessionService _userSession;
        private readonly INotificacionService _notificacionService;

        public ProductosController(
            AppDbContext context,
            IUserSessionService userSession,
            INotificacionService notificacionService)
        {
            _context = context;
            _userSession = userSession;
            _notificacionService = notificacionService;
        }

        // GET: Productos
        public async Task<IActionResult> Index(int? emprendimientoId, int? categoriaId)
        {
            var productos = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Emprendimiento)
                .ThenInclude(e => e!.Usuario)
                .AsQueryable();

            if (emprendimientoId.HasValue)
            {
                productos = productos.Where(p => p.EmprendimientoId == emprendimientoId.Value);
                if (!_userSession.IsAdmin)
                {
                    productos = productos.Where(p => p.Activo && p.Emprendimiento!.Activo);
                }

                var emprendimiento = await _context.Emprendimientos.FindAsync(emprendimientoId.Value);
                if (emprendimiento != null)
                {
                    ViewData["SelectedEmprendimientoNombre"] = emprendimiento.Nombre;
                }
            }
            else if (categoriaId.HasValue)
            {
                productos = productos.Where(p => p.CategoriaId == categoriaId.Value);
                if (!_userSession.IsAdmin)
                {
                    productos = productos.Where(p => p.Activo && p.Emprendimiento!.Activo);
                }

                var categoria = await _context.Categorias.FindAsync(categoriaId.Value);
                if (categoria != null)
                {
                    ViewData["SelectedCategoriaNombre"] = categoria.Nombre;
                }
            }
            else
            {
                if (_userSession.IsEntrepreneur)
                {
                    var currentUserId = _userSession.GetCurrentUserId();
                    productos = productos.Where(p => p.Emprendimiento!.UsuarioId == currentUserId);
                }
                else if (!_userSession.IsAdmin)
                {
                    productos = productos.Where(p => p.Activo && p.Emprendimiento!.Activo);
                }
            }

            if (_userSession.IsAdmin)
            {
                productos = productos.IgnoreQueryFilters();
            }

            return View(await productos.ToListAsync());
        }
        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Emprendimiento)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (producto == null || !CanView(producto))
            {
                return NotFound();
            }

            return View(producto);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            if (!_userSession.IsAdmin && !_userSession.IsEntrepreneur)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            SetSelectLists();
            return View();
        }

        // POST: Productos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Descripcion,Precio,Stock,Activo,FechaCreacion,EmprendimientoId,CategoriaId")] Producto producto)
        {
            if (!_userSession.IsAdmin && !_userSession.IsEntrepreneur)
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (!await CanUseEmprendimientoAsync(producto.EmprendimientoId))
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (ModelState.IsValid)
            {
                producto.FechaCreacion = DateTime.UtcNow;
                producto.Activo = true;
                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            SetSelectLists(producto.CategoriaId, producto.EmprendimientoId);
            return View(producto);
        }

        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null || !await CanManageAsync(producto.Id))
            {
                return NotFound();
            }
            SetSelectLists(producto.CategoriaId, producto.EmprendimientoId);
            return View(producto);
        }

        // POST: Productos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion,Precio,Stock,Activo,FechaCreacion,EmprendimientoId,CategoriaId")] Producto producto)
        {
            if (id != producto.Id)
            {
                return NotFound();
            }

            var existing = await _context.Productos.FindAsync(id);
            if (existing == null || !await CanManageAsync(existing.Id) || !await CanUseEmprendimientoAsync(producto.EmprendimientoId))
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var stockChanged = existing.Stock != producto.Stock;
                    existing.Nombre = producto.Nombre;
                    existing.Descripcion = producto.Descripcion;
                    existing.Precio = producto.Precio;
                    existing.Stock = producto.Stock;
                    existing.Activo = producto.Activo;
                    existing.FechaCreacion = producto.FechaCreacion;
                    existing.CategoriaId = producto.CategoriaId;
                    existing.EmprendimientoId = producto.EmprendimientoId;

                    await _context.SaveChangesAsync();
                    if (stockChanged)
                    {
                        await _notificacionService.StockActualizadoAsync(
                            existing.Id,
                            existing.Nombre,
                            existing.Stock);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.Id))
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
            SetSelectLists(producto.CategoriaId, producto.EmprendimientoId);
            return View(producto);
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Emprendimiento)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (producto == null || !await CanManageAsync(producto.Id))
            {
                return NotFound();
            }

            return View(producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null && await CanManageAsync(producto.Id))
            {
                producto.Activo = false;
                producto.FechaEliminacion = DateTime.UtcNow;
                producto.EliminadoPor = User.Identity?.Name;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }

        private bool CanView(Producto producto)
        {
            if (_userSession.IsAdmin) return true;
            if (_userSession.IsEntrepreneur) return producto.Emprendimiento?.UsuarioId == _userSession.GetCurrentUserId();
            return producto.Activo && producto.Emprendimiento?.Activo == true;
        }

        private async Task<bool> CanManageAsync(int productoId)
        {
            if (_userSession.IsAdmin) return true;

            var currentUserId = _userSession.GetCurrentUserId();
            return _userSession.IsEntrepreneur && await _context.Productos
                .Include(p => p.Emprendimiento)
                .AnyAsync(p => p.Id == productoId && p.Emprendimiento!.UsuarioId == currentUserId);
        }

        private async Task<bool> CanUseEmprendimientoAsync(int emprendimientoId)
        {
            if (_userSession.IsAdmin) return true;

            var currentUserId = _userSession.GetCurrentUserId();
            return _userSession.IsEntrepreneur && await _context.Emprendimientos
                .AnyAsync(e => e.Id == emprendimientoId && e.UsuarioId == currentUserId && e.Activo);
        }

        private void SetSelectLists(int? categoriaId = null, int? emprendimientoId = null)
        {
            var categorias = _context.Categorias.Where(c => c.Activo || _userSession.IsAdmin);
            var emprendimientos = _context.Emprendimientos.AsQueryable();

            if (_userSession.IsEntrepreneur)
            {
                var currentUserId = _userSession.GetCurrentUserId();
                emprendimientos = emprendimientos.Where(e => e.UsuarioId == currentUserId && e.Activo);
            }

            ViewData["CategoriaId"] = new SelectList(categorias, "Id", "Nombre", categoriaId);
            ViewData["EmprendimientoId"] = new SelectList(emprendimientos, "Id", "Nombre", emprendimientoId);
        }
    }
}
