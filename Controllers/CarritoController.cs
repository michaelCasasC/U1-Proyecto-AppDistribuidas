using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Services;
using Proyecto.ViewModels;

namespace Proyecto.Controllers
{
    public class CarritoController : Controller
    {
        private const string CartSessionKey = "ShoppingCart";
        private readonly AppDbContext _context;
        private readonly IUserSessionService _userSession;
        private readonly INotificacionService _notificacionService;

        public CarritoController(
            AppDbContext context,
            IUserSessionService userSession,
            INotificacionService notificacionService)
        {
            _context = context;
            _userSession = userSession;
            _notificacionService = notificacionService;
        }

        public async Task<IActionResult> Index()
        {
            var accessResult = EnsureCanShop();
            if (accessResult != null) return accessResult;

            var items = await GetCartItemsAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productoId, int cantidad = 1)
        {
            var accessResult = EnsureCanShop();
            if (accessResult != null) return accessResult;

            var producto = await _context.Productos
                .Include(p => p.Emprendimiento)
                .FirstOrDefaultAsync(p => p.Id == productoId && p.Activo && p.Stock > 0 && p.Emprendimiento!.Activo);

            if (producto == null)
            {
                return NotFound();
            }

            var cart = GetCart();
            var newQuantity = cart.GetValueOrDefault(productoId) + Math.Max(1, cantidad);
            cart[productoId] = Math.Min(newQuantity, producto.Stock);
            SaveCart(cart);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                var totalItems = cart.Values.Sum();
                return Json(new { success = true, cartCount = totalItems });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int productoId, int cantidad)
        {
            var accessResult = EnsureCanShop();
            if (accessResult != null) return accessResult;

            var cart = GetCart();
            if (!cart.ContainsKey(productoId))
            {
                return RedirectToAction(nameof(Index));
            }

            if (cantidad <= 0)
            {
                cart.Remove(productoId);
            }
            else
            {
                var stock = await _context.Productos
                    .Where(p => p.Id == productoId && p.Activo)
                    .Select(p => p.Stock)
                    .FirstOrDefaultAsync();

                cart[productoId] = Math.Min(cantidad, stock);
            }

            SaveCart(cart.Where(item => item.Value > 0).ToDictionary(item => item.Key, item => item.Value));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productoId)
        {
            var accessResult = EnsureCanShop();
            if (accessResult != null) return accessResult;

            var cart = GetCart();
            cart.Remove(productoId);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var accessResult = EnsureCanShop();
            if (accessResult != null) return accessResult;

            var currentUserId = _userSession.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Carrito") });
            }

            var cart = GetCart();
            if (cart.Count == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var productos = await _context.Productos
                .Include(p => p.Emprendimiento)
                .Where(p => cart.Keys.Contains(p.Id) && p.Activo && p.Emprendimiento!.Activo)
                .ToListAsync();

            if (productos.Count != cart.Count)
            {
                ModelState.AddModelError(string.Empty, "Algunos productos ya no están disponibles.");
                return View(nameof(Index), await GetCartItemsAsync());
            }

            var pedido = new Models.Pedido
            {
                UsuarioId = currentUserId.Value,
                FechaPedido = DateTime.UtcNow,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            foreach (var producto in productos)
            {
                var cantidad = cart[producto.Id];
                if (cantidad > producto.Stock)
                {
                    ModelState.AddModelError(string.Empty, $"Stock insuficiente para {producto.Nombre}.");
                    return View(nameof(Index), await GetCartItemsAsync());
                }

                pedido.Detalles.Add(new Models.DetallePedido
                {
                    ProductoId = producto.Id,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.Precio,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }

            pedido.Total = pedido.Detalles.Sum(detalle => detalle.Total);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Error al crear el pedido. Intenta de nuevo.");
                return View(nameof(Index), await GetCartItemsAsync());
            }

            SaveCart(new Dictionary<int, int>());
            await _notificacionService.NuevoPedidoAsync(
                pedido.Id,
                pedido.Total,
                productos.Select(producto => producto.EmprendimientoId));

            return RedirectToAction("CreateOrder", "Payment", new { pedidoId = pedido.Id });
        }

        private async Task<List<CartItemViewModel>> GetCartItemsAsync()
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                return new List<CartItemViewModel>();
            }

            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Emprendimiento)
                .Where(p => cart.Keys.Contains(p.Id))
                .ToListAsync();

            return productos.Select(producto => new CartItemViewModel
            {
                Producto = producto,
                Cantidad = Math.Min(cart[producto.Id], producto.Stock)
            }).ToList();
        }

        private Dictionary<int, int> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrWhiteSpace(json)
                ? new Dictionary<int, int>()
                : JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
        }

        private void SaveCart(Dictionary<int, int> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        private IActionResult? EnsureCanShop()
        {
            if (!_userSession.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }

            return _userSession.CanShop ? null : RedirectToAction("AccessDenied", "Home");
        }
    }
}
