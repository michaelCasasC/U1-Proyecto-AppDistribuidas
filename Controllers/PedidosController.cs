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
using Proyecto.ViewModels;

namespace Proyecto.Controllers
{
    public class PedidosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserSessionService _userSession;

        public PedidosController(AppDbContext context, IUserSessionService userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        // GET: Pedidos
        public async Task<IActionResult> Index(int page = 1)
        {
            var accessResult = EnsureLoggedIn();
            if (accessResult != null)
                return accessResult;

            const int pageSize = 50;

            var pedidosQuery = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .AsQueryable();

            if (_userSession.IsEntrepreneur)
            {
                var currentUserId = _userSession.GetCurrentUserId();

                pedidosQuery = pedidosQuery.Where(p =>
                    p.Detalles.Any(d =>
                        d.Producto!.Emprendimiento!.UsuarioId == currentUserId));
            }
            else if (!_userSession.IsAdmin)
            {
                var currentUserId = _userSession.GetCurrentUserId();

                pedidosQuery = pedidosQuery.Where(p =>
                    p.UsuarioId == currentUserId);
            }

            if (_userSession.IsAdmin)
            {
                pedidosQuery = pedidosQuery.IgnoreQueryFilters();
            }

            var totalRecords = await pedidosQuery.CountAsync();

            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            if (page < 1)
                page = 1;

            if (page > totalPages && totalPages > 0)
                page = totalPages;

            var pedidos = await pedidosQuery
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new PedidoIndexViewModel
            {
                Pedidos = pedidos,
                CurrentPage = page,
                TotalRecords = totalRecords,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return View(vm);
        }

// GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var accessResult = EnsureLoggedIn();
            if (accessResult != null) return accessResult;

            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p!.Emprendimiento)
                .Include(p => p.Pagos)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pedido == null || !CanView(pedido))
            {
                return NotFound();
            }

            return View(pedido);
        }

        // GET: Pedidos/Create
        public IActionResult Create()
        {
            return RedirectToAction("Index", "Carrito");
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Id,FechaPedido,Total,Activo,FechaCreacion")] Pedido pedido)
        {
            if (!ModelState.IsValid)
            {
                return View(pedido);
            }

            return RedirectToAction("Index", "Carrito");
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Email", pedido.UsuarioId);
            return View(pedido);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UsuarioId,FechaPedido,Total,Activo,FechaCreacion")] Pedido pedido)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id != pedido.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Pedidos.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.UsuarioId = pedido.UsuarioId;
                    existing.FechaPedido = pedido.FechaPedido;
                    existing.Total = pedido.Total;
                    existing.Activo = pedido.Activo;
                    existing.FechaCreacion = pedido.FechaCreacion;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExists(pedido.Id))
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
            ViewData["UsuarioId"] = new SelectList(_context.Users, "Id", "Email", pedido.UsuarioId);
            return View(pedido);
        }

        // GET: Pedidos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }

        // POST: Pedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var adminResult = EnsureAdmin();
            if (adminResult != null) return adminResult;

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                pedido.Activo = false;
                pedido.FechaEliminacion = DateTime.UtcNow;
                pedido.EliminadoPor = User.Identity?.Name;
                var detalles = await _context.DetallesPedido.Where(d => d.PedidoId == pedido.Id).ToListAsync();
                foreach (var detalle in detalles)
                {
                    detalle.Activo = false;
                    detalle.FechaEliminacion = DateTime.UtcNow;
                    detalle.EliminadoPor = User.Identity?.Name;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.Id == id);
        }

        private IActionResult? EnsureAdmin()
        {
            if (!_userSession.IsLoggedIn)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            if (!_userSession.IsAdmin)
            {
      
                _userSession.Clear();
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            return null;
        }

        private IActionResult? EnsureLoggedIn()
        {
            if (!_userSession.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            return null;
        }

        private bool CanView(Pedido pedido)
        {
            if (_userSession.IsAdmin) return true;

            var currentUserId = _userSession.GetCurrentUserId();
            if (_userSession.IsEntrepreneur)
            {
                return pedido.Detalles.Any(d => d.Producto?.Emprendimiento?.UsuarioId == currentUserId);
            }

            return pedido.UsuarioId == currentUserId;
        }
    }
}