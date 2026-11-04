using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Models;
using Proyecto.Services;
using System.Text.Json;

namespace Proyecto.Controllers
{
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IUserSessionService _userSession;
        private readonly IPayPalService _payPalService;
        private readonly IPayPhoneService _payPhoneService;
        private readonly IInventoryService _inventoryService;
        private readonly IAuditService _auditService;
        private readonly INotificacionService _notificacionService;
        private readonly IConfiguration _configuration;

        public PaymentController(
            AppDbContext context,
            IUserSessionService userSession,
            IPayPalService payPalService,
            IPayPhoneService payPhoneService,
            IInventoryService inventoryService,
            IAuditService auditService,
            INotificacionService notificacionService,
            IConfiguration configuration)
        {
            _context = context;
            _userSession = userSession;
            _payPalService = payPalService;
            _payPhoneService = payPhoneService;
            _inventoryService = inventoryService;
            _auditService = auditService;
            _notificacionService = notificacionService;
            _configuration = configuration;
        }

        // GET: Payment/CreateOrder - Show payment method selection
        [HttpGet]
        public async Task<IActionResult> CreateOrder(int pedidoId)
        {
            var accessResult = EnsureLoggedIn();
            if (accessResult != null) return accessResult;

            var currentUserId = _userSession.GetCurrentUserId();
            if (!currentUserId.HasValue) return RedirectToAction("Login", "Account");

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Pagos)
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == currentUserId.Value);

            if (pedido == null) return NotFound();

            if (pedido.Pagos.Any(p => p.Estado == "COMPLETED" || p.Estado == "APPROVED" || p.Estado == "PAID"))
            {
                TempData["Error"] = "Este pedido ya tiene un pago completado.";
                return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
            }

            return View(pedido);
        }

        // POST: Payment/PayPal/Create - Initiate PayPal payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayPalCreate(int pedidoId)
        {
            var accessResult = EnsureLoggedIn();
            if (accessResult != null) return accessResult;

            var currentUserId = _userSession.GetCurrentUserId();
            if (!currentUserId.HasValue) return RedirectToAction("Login", "Account");

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Pagos)
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == currentUserId.Value);

            if (pedido == null) return NotFound();

            if (pedido.Pagos.Any(p => p.Estado == "COMPLETED" || p.Estado == "APPROVED" || p.Estado == "PAID"))
            {
                TempData["Error"] = "Este pedido ya tiene un pago completado.";
                return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
            }

            try
            {
                var returnUrl = Url.Action("PayPalSuccess", "Payment", new { pedidoId }, Request.Scheme)!;
                var cancelUrl = Url.Action("Cancel", "Payment", new { pedidoId }, Request.Scheme)!;

                var orderId = await _payPalService.CreateOrderAsync(pedido.Total, "USD", returnUrl, cancelUrl, pedidoId.ToString());

                var pago = new Pago
                {
                    PedidoId = pedido.Id,
                    PayPalOrderId = orderId,
                    Monto = pedido.Total,
                    Moneda = "USD",
                    Estado = "CREATED",
                    MetodoPago = "PayPal",
                    FechaCreacion = DateTime.UtcNow
                };

                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();

                var approvalUrl = $"https://www.sandbox.paypal.com/checkoutnow?token={orderId}";
                return Redirect(approvalUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear la orden de PayPal: {ex.Message}";
                return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
            }
        }

        // GET: Payment/PayPal/Success - Handle PayPal return
        public async Task<IActionResult> PayPalSuccess(int pedidoId, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Token de PayPal no recibido.";
                return RedirectToAction("Index", "Pedidos");
            }

            var accessResult = EnsureLoggedIn();
            if (accessResult != null) return accessResult;

            var currentUserId = _userSession.GetCurrentUserId();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pago = await _context.Pagos
                    .Include(p => p.Pedido)
                        .ThenInclude(ped => ped!.Detalles)
                            .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.PayPalOrderId == token);

                if (pago == null)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "No se encontró el pago asociado.";
                    return RedirectToAction("Index", "Pedidos");
                }

                if (currentUserId.HasValue && pago.Pedido?.UsuarioId != currentUserId.Value && !_userSession.IsAdmin)
                {
                    await transaction.RollbackAsync();
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Idempotency: si ya fue procesado, retornar éxito sin reprocesar
                if (pago.Estado == "COMPLETED" || pago.Estado == "APPROVED" || pago.Estado == "PAID")
                {
                    await transaction.RollbackAsync();
                    TempData["Mensaje"] = "El pago ya fue procesado anteriormente.";
                    return RedirectToAction("Details", "Pedidos", new { id = pago.PedidoId });
                }

                pago.IntentosVerificacion++;
                pago.UltimoIntentoVerificacion = DateTime.UtcNow;

                var captureResult = await _payPalService.CaptureOrderAsync(token);

                pago.FechaActualizacion = DateTime.UtcNow;
                pago.PayPalPayerId = captureResult.PayerId;
                pago.PayPalPaymentId = captureResult.CaptureId;
                pago.DetallesJson = JsonSerializer.Serialize(captureResult);

                if (captureResult.Success && (captureResult.Status == "COMPLETED" || captureResult.Status == "APPROVED"))
                {
                    pago.Estado = captureResult.Status;

                    await _inventoryService.DeductStockAsync(pago.PedidoId, currentUserId);
                    await _notificacionService.EstadoPedidoCambiadoAsync(
                        pago.PedidoId,
                        pago.Estado,
                        pago.Pedido!.UsuarioId);

                    pago.Pedido!.Activo = true;
                    _context.Pedidos.Update(pago.Pedido);

                    await _auditService.LogAsync("Pago PayPal completado", "Pedido", pago.PedidoId, currentUserId);
                    TempData["Mensaje"] = "¡Pago completado exitosamente! Tu pedido está confirmado.";
                }
                else
                {
                    pago.Estado = "FAILED";
                    await _notificacionService.EstadoPedidoCambiadoAsync(
                        pago.PedidoId,
                        pago.Estado,
                        pago.Pedido!.UsuarioId);
                    TempData["Error"] = $"El pago no se pudo completar: {captureResult.ErrorMessage ?? captureResult.Status}";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                var pago = await _context.Pagos.FirstOrDefaultAsync(p => p.PayPalOrderId == token);
                if (pago != null)
                {
                    pago.Estado = "ERROR";
                    pago.DetallesJson = ex.Message;
                    pago.IntentosVerificacion++;
                    pago.UltimoIntentoVerificacion = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                TempData["Error"] = $"Error al procesar el pago: {ex.Message}";
            }

            var pagoId = (await _context.Pagos.FirstOrDefaultAsync(p => p.PayPalOrderId == token))?.PedidoId ?? pedidoId;
            return RedirectToAction("Details", "Pedidos", new { id = pagoId });
        }

        // POST: Payment/PayPhone/Create - Initiate PayPhone payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayPhoneCreate(int pedidoId)
        {
            var accessResult = EnsureLoggedIn();
            if (accessResult != null) return accessResult;

            var currentUserId = _userSession.GetCurrentUserId();
            if (!currentUserId.HasValue) return RedirectToAction("Login", "Account");

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.Pagos)
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.UsuarioId == currentUserId.Value);

            if (pedido == null) return NotFound();

            if (pedido.Pagos.Any(p => p.Estado == "COMPLETED" || p.Estado == "APPROVED" || p.Estado == "PAID"))
            {
                TempData["Error"] = "Este pedido ya tiene un pago completado.";
                return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
            }

            try
            {
                if (pedido.Total < 1.00m)
                {
                    TempData["Error"] = "No se puede generar el link porque el total es menor a $1.00.";
                    return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
                }

                var clientTxnId = DateTime.Now.ToString("yyMMddHHmmssfff")[..15];

                var result = await _payPhoneService.CreatePaymentLinkAsync(
                    pedido.Total,
                    $"Pedido #{pedidoId}",
                    clientTxnId);

                if (!result.Success)
                {
                    TempData["Error"] = $"Error al crear el link de pago PayPhone: {result.ErrorMessage}";
                    return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
                }

                var pago = new Pago
                {
                    PedidoId = pedido.Id,
                    PayPhoneClientTransactionId = clientTxnId,
                    PayPhoneTransactionId = result.PaymentId,
                    PayPhonePaymentUrl = result.PaymentUrl,
                    Monto = pedido.Total,
                    Moneda = "USD",
                    Estado = "CREATED",
                    MetodoPago = "PayPhone",
                    FechaCreacion = DateTime.UtcNow
                };

                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();

                return Redirect(result.PaymentUrl!);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al crear el link de PayPhone: {ex.Message}";
                return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
            }
        }

        // GET: Payment/PayPhone/Return - Handle PayPhone return
        public async Task<IActionResult> PayPhoneReturn(int pedidoId)
        {
            var accessResult = EnsureLoggedIn();
            if (accessResult != null) return accessResult;

            var currentUserId = _userSession.GetCurrentUserId();

            var transactionId = Request.Query["transactionId"].FirstOrDefault();
            var clientTransactionId = Request.Query["clientTransactionId"].FirstOrDefault();
            var status = Request.Query["status"].FirstOrDefault();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pago = await _context.Pagos
                    .Include(p => p.Pedido)
                        .ThenInclude(ped => ped!.Detalles)
                            .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.PedidoId == pedidoId && 
                        (p.PayPhoneClientTransactionId == clientTransactionId || p.PayPhoneTransactionId == transactionId));

                if (pago == null)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "No se encontró el pago asociado.";
                    return RedirectToAction("Index", "Pedidos");
                }

                pago.IntentosVerificacion++;
                pago.UltimoIntentoVerificacion = DateTime.UtcNow;
                pago.FechaActualizacion = DateTime.UtcNow;

                // Idempotency: si ya está pagado, no reprocesar
                if (pago.Estado == "COMPLETED" || pago.Estado == "APPROVED" || pago.Estado == "PAID")
                {
                    await transaction.RollbackAsync();
                    TempData["Mensaje"] = "El pago ya fue procesado anteriormente.";
                    return RedirectToAction("Details", "Pedidos", new { id = pago.PedidoId });
                }

                if (status == "paid" || status == "completed" || status == "approved")
                {
                    pago.Estado = "PAID";
                    pago.PayPhoneTransactionId = transactionId;

                    await _inventoryService.DeductStockAsync(pago.PedidoId, currentUserId);
                    await _notificacionService.EstadoPedidoCambiadoAsync(
                        pago.PedidoId,
                        pago.Estado,
                        pago.Pedido!.UsuarioId);

                    pago.Pedido!.Activo = true;
                    _context.Pedidos.Update(pago.Pedido);

                    await _auditService.LogAsync("Pago PayPhone completado", "Pedido", pago.PedidoId, currentUserId);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Mensaje"] = "¡Pago completado exitosamente con PayPhone! Tu pedido está confirmado.";
                }
                else
                {
                    pago.Estado = "FAILED";
                    await _notificacionService.EstadoPedidoCambiadoAsync(
                        pago.PedidoId,
                        pago.Estado,
                        pago.Pedido!.UsuarioId);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["Error"] = $"El pago no se completó: {status}";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"Error al procesar el pago PayPhone: {ex.Message}";
            }

            return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
        }

        // GET: Payment/PayPhone/Cancel - Handle PayPhone cancel
        public IActionResult PayPhoneCancel(int pedidoId)
        {
            TempData["Mensaje"] = "Pago cancelado por el usuario.";
            return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
        }

        // GET: Payment/Cancel - Generic cancel
        public IActionResult Cancel(int pedidoId)
        {
            TempData["Mensaje"] = "Pago cancelado por el usuario.";
            return RedirectToAction("Details", "Pedidos", new { id = pedidoId });
        }

        private IActionResult? EnsureLoggedIn()
        {
            if (!_userSession.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = $"{Request.Path}{Request.QueryString}" });
            }
            return null;
        }
    }
}