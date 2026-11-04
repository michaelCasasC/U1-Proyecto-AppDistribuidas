using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Models;
using Proyecto.Services;
using Proyecto.ViewModels.Reports;

namespace Proyecto.Controllers;

public class ReportsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IUserSessionService _userSession;

    public ReportsController(AppDbContext context, IUserSessionService userSession)
    {
        _context = context;
        _userSession = userSession;
    }

    private IActionResult? EnsureAdmin()
    {
        if (!_userSession.IsLoggedIn)
            return RedirectToAction("Login", "Account", new { returnUrl = $"{Request.Path}{Request.QueryString}" });
        if (!_userSession.IsAdmin)
            return RedirectToAction("AccessDenied", "Home");
        return null;
    }

    public async Task<IActionResult> Index()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var model = new ReporteDashboardViewModel
        {
            TotalPedidos = await _context.Pedidos.CountAsync(),
            PedidosActivos = await _context.Pedidos.CountAsync(p => p.Activo),
            IngresosTotales = await _context.Pagos.Where(p => p.Estado == "COMPLETED" || p.Estado == "APPROVED" || p.Estado == "PAID").SumAsync(p => p.Monto),
            TotalClientes = await _context.Users.CountAsync(u => u.EsCliente),
            TotalEmprendimientos = await _context.Emprendimientos.CountAsync(),
            TotalProductos = await _context.Productos.CountAsync(),
            ProductosStockBajo = await _context.Productos.CountAsync(p => p.Activo && p.Stock <= 10),
            TotalPagosExitosos = await _context.Pagos.CountAsync(p => p.Estado == "COMPLETED" || p.Estado == "APPROVED" || p.Estado == "PAID"),
            TotalUsuarios = await _context.Users.CountAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> VentasPorFecha(DateTime? fechaInicio, DateTime? fechaFin)
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var modelo = new VentasPorFechaViewModel
        {
            FechaInicio = DateTime.SpecifyKind(fechaInicio ?? DateTime.UtcNow.AddMonths(-1).Date, DateTimeKind.Utc),
            FechaFin = DateTime.SpecifyKind(fechaFin ?? DateTime.UtcNow.Date, DateTimeKind.Utc)
        };

        var query = _context.Pedidos
            .Where(p => p.FechaPedido >= modelo.FechaInicio && p.FechaPedido <= modelo.FechaFin.AddDays(1))
            .GroupBy(p => p.FechaPedido.Date)
            .Select(g => new VentasPorFechaItem
            {
                Fecha = g.Key,
                CantidadPedidos = g.Count(),
                TotalVentas = g.Sum(p => p.Total)
            })
            .OrderByDescending(i => i.Fecha);

        modelo.Items = await query.ToListAsync();
        return View(modelo);
    }

    public async Task<IActionResult> VentasPorEmprendimiento()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var query = _context.DetallesPedido
            .Include(d => d.Producto!)
            .ThenInclude(p => p.Emprendimiento!)
            .ThenInclude(e => e.Usuario)
            .Where(d => d.Pedido!.Activo)
            .GroupBy(d => new { d.Producto!.EmprendimientoId, Emprendimiento = d.Producto.Emprendimiento!.Nombre, Usuario = d.Producto.Emprendimiento.Usuario!.Nombre })
            .Select(g => new VentasPorEmprendimientoItem
            {
                Emprendimiento = g.Key.Emprendimiento,
                Usuario = g.Key.Usuario,
                CantidadVentas = g.Sum(d => d.Cantidad),
                TotalVentas = g.Sum(d => d.Total)
            })
            .OrderByDescending(i => i.TotalVentas);

        var model = new VentasPorEmprendimientoViewModel();
        model.Items = await query.ToListAsync();
        return View(model);
    }

    public async Task<IActionResult> VentasPorUsuario()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var query = _context.Pedidos
            .Include(p => p.Usuario)
            .Where(p => p.Activo)
            .GroupBy(p => new { p.UsuarioId, p.Usuario!.Nombre, p.Usuario.Email })
            .Select(g => new VentasPorUsuarioItem
            {
                UsuarioId = g.Key.UsuarioId,
                Nombre = g.Key.Nombre ?? "",
                Email = g.Key.Email ?? "",
                CantidadPedidos = g.Count(),
                TotalGastado = g.Sum(p => p.Total)
            })
            .OrderByDescending(i => i.TotalGastado);

        var model = new VentasPorUsuarioViewModel();
        model.Items = await query.ToListAsync();
        return View(model);
    }

    public async Task<IActionResult> ProductosMasVendidos()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var query = _context.DetallesPedido
            .Include(d => d.Producto!)
            .ThenInclude(p => p.Categoria)
            .Include(d => d.Producto!)
            .ThenInclude(p => p.Emprendimiento)
            .Where(d => d.Pedido!.Activo)
            .GroupBy(d => new { d.ProductoId, d.Producto!.Nombre, Categoria = d.Producto.Categoria!.Nombre, Emprendimiento = d.Producto.Emprendimiento!.Nombre })
            .Select(g => new ProductosMasVendidosItem
            {
                Producto = g.Key.Nombre,
                Categoria = g.Key.Categoria,
                Emprendimiento = g.Key.Emprendimiento,
                CantidadVendida = g.Sum(d => d.Cantidad),
                TotalGenerado = g.Sum(d => d.Cantidad * d.PrecioUnitario)
            })
            .OrderByDescending(i => i.CantidadVendida)
            .Take(50);

        var model = new ProductosMasVendidosViewModel();
        model.Items = await query.ToListAsync();
        return View(model);
    }

    public async Task<IActionResult> ProductosBajoInventario(int stockMinimo = 10)
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var query = _context.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Emprendimiento)
            .Where(p => p.Activo && p.Stock <= stockMinimo)
            .Select(p => new ProductosBajoInventarioItem
            {
                ProductoId = p.Id,
                Producto = p.Nombre,
                Categoria = p.Categoria!.Nombre,
                Emprendimiento = p.Emprendimiento!.Nombre,
                Stock = p.Stock,
                Precio = p.Precio
            })
            .OrderBy(i => i.Stock);

        var model = new ProductosBajoInventarioViewModel { StockMinimo = stockMinimo };
        model.Items = await query.ToListAsync();
        return View(model);
    }

    public async Task<IActionResult> MovimientosInventario(DateTime? fechaInicio, DateTime? fechaFin)
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var modelo = new MovimientosInventarioViewModel
        {
            FechaInicio = DateTime.SpecifyKind(fechaInicio ?? DateTime.UtcNow.AddMonths(-1).Date, DateTimeKind.Utc),
            FechaFin = DateTime.SpecifyKind(fechaFin ?? DateTime.UtcNow.Date, DateTimeKind.Utc)
        };

        var query = _context.DetallesPedido
            .Include(d => d.Producto!)
            .ThenInclude(p => p.Emprendimiento)
            .Include(d => d.Pedido)
            .Where(d => d.Pedido!.FechaPedido >= modelo.FechaInicio && d.Pedido.FechaPedido <= modelo.FechaFin.AddDays(1))
            .Select(d => new MovimientosInventarioItem
            {
                Fecha = d.Pedido!.FechaPedido,
                Producto = d.Producto!.Nombre,
                Emprendimiento = d.Producto.Emprendimiento!.Nombre,
                Cantidad = d.Cantidad,
                Tipo = "Venta",
                PedidoId = d.PedidoId
            })
            .OrderByDescending(i => i.Fecha)
            .Take(500);

        modelo.Items = await query.ToListAsync();
        return View(modelo);
    }

    public async Task<IActionResult> PagosPorPasarela()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var query = _context.Pagos
            .GroupBy(p => p.MetodoPago)
            .Select(g => new PagosPorPasarelaItem
            {
                MetodoPago = g.Key ?? "N/A",
                Cantidad = g.Count(),
                MontoTotal = g.Sum(p => p.Monto),
                Aprobados = g.Count(p => p.Estado == "COMPLETED" || p.Estado == "APPROVED" || p.Estado == "PAID"),
                Fallidos = g.Count(p => p.Estado == "FAILED" || p.Estado == "ERROR" || p.Estado == "CANCELLED")
            })
            .OrderByDescending(i => i.Cantidad);

        var model = new PagosPorPasarelaViewModel();
        model.Items = await query.ToListAsync();
        return View(model);
    }

    public async Task<IActionResult> PagosPorEstado()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var query = _context.Pagos
            .GroupBy(p => p.Estado)
            .Select(g => new PagosPorEstadoItem
            {
                Estado = g.Key,
                Cantidad = g.Count(),
                MontoTotal = g.Sum(p => p.Monto)
            })
            .OrderByDescending(i => i.Cantidad);

        var model = new PagosPorEstadoViewModel();
        model.Items = await query.ToListAsync();
        return View(model);
    }

    public async Task<IActionResult> ClientesTop()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var query = _context.Pedidos
            .Include(p => p.Usuario)
            .Where(p => p.Activo)
            .GroupBy(p => new { p.UsuarioId, p.Usuario!.Nombre, p.Usuario.Email })
            .Select(g => new ClientesTopItem
            {
                UsuarioId = g.Key.UsuarioId,
                Nombre = g.Key.Nombre ?? "",
                Email = g.Key.Email ?? "",
                CantidadCompras = g.Count(),
                TotalGastado = g.Sum(p => p.Total)
            })
            .OrderByDescending(i => i.TotalGastado)
            .Take(20);

        var model = new ClientesTopViewModel();
        model.Items = await query.ToListAsync();
        return View(model);
    }

    public async Task<IActionResult> PromedioVentaCliente()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var totalPedidos = await _context.Pedidos.CountAsync(p => p.Activo);
        var ingresoTotal = await _context.Pedidos.Where(p => p.Activo).SumAsync(p => p.Total);
        var clientesConCompras = await _context.Pedidos.Where(p => p.Activo).Select(p => p.UsuarioId).Distinct().CountAsync();
        var promedio = clientesConCompras > 0 ? ingresoTotal / clientesConCompras : 0;

        var items = await _context.Pedidos
            .Include(p => p.Usuario)
            .Where(p => p.Activo)
            .GroupBy(p => new { p.UsuarioId, p.Usuario!.Nombre, p.Usuario.Email })
            .Select(g => new PromedioVentaClienteItem
            {
                UsuarioId = g.Key.UsuarioId,
                Nombre = g.Key.Nombre ?? "",
                Email = g.Key.Email ?? "",
                CantidadCompras = g.Count(),
                TotalGastado = g.Sum(p => p.Total),
                Promedio = g.Count() > 0 ? g.Sum(p => p.Total) / g.Count() : 0
            })
            .OrderByDescending(i => i.TotalGastado)
            .ToListAsync();

        var model = new PromedioVentaClienteViewModel
        {
            PromedioGlobal = Math.Round(promedio, 2),
            TotalClientesConCompras = clientesConCompras,
            TotalPedidos = totalPedidos,
            IngresoTotal = ingresoTotal,
            Items = items
        };

        return View(model);
    }

    public async Task<IActionResult> IngresosPorMes()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var query = _context.Pedidos
            .Where(p => p.Activo)
            .GroupBy(p => new { p.FechaPedido.Year, p.FechaPedido.Month })
            .Select(g => new IngresosPorMesItem
            {
                Anio = g.Key.Year,
                Mes = g.Key.Month,
                MesNombre = "",
                CantidadPedidos = g.Count(),
                TotalIngresos = g.Sum(p => p.Total)
            })
            .OrderByDescending(i => i.Anio).ThenByDescending(i => i.Mes);

        var items = await query.ToListAsync();
        var cultura = System.Globalization.CultureInfo.GetCultureInfo("es-ES");
        foreach (var item in items)
            item.MesNombre = cultura.DateTimeFormat.GetMonthName(item.Mes);

        var reverseItems = Enumerable.Reverse(items).ToList();
        var chartLabels = string.Join(",", reverseItems.Select(i => $"'{i.MesNombre} {i.Anio}'"));
        var chartIngresos = string.Join(",", reverseItems.Select(i => i.TotalIngresos.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        var chartPedidos = string.Join(",", reverseItems.Select(i => i.CantidadPedidos.ToString()));

        var model = new IngresosPorMesViewModel
        {
            Items = items,
            ChartLabels = chartLabels,
            ChartIngresos = chartIngresos,
            ChartPedidos = chartPedidos
        };
        return View(model);
    }

    public async Task<IActionResult> RegistrosActivos()
    {
        var accessResult = EnsureAdmin();
        if (accessResult != null) return accessResult;

        var usuariosActivos = await _context.Users.CountAsync(u => u.Activo);
        var usuariosInactivos = await _context.Users.IgnoreQueryFilters().CountAsync(u => !u.Activo || u.FechaEliminacion != null);

        var emprendimientosActivos = await _context.Emprendimientos.CountAsync(e => e.Activo);
        var emprendimientosInactivos = await _context.Emprendimientos.IgnoreQueryFilters().CountAsync(e => !e.Activo || e.FechaEliminacion != null);

        var productosActivos = await _context.Productos.CountAsync(p => p.Activo);
        var productosInactivos = await _context.Productos.IgnoreQueryFilters().CountAsync(p => !p.Activo || p.FechaEliminacion != null);

        var pedidosActivos = await _context.Pedidos.CountAsync(p => p.Activo);
        var pedidosInactivos = await _context.Pedidos.IgnoreQueryFilters().CountAsync(p => !p.Activo || p.FechaEliminacion != null);

        var model = new RegistrosActivosViewModel
        {
            Items = new List<RegistrosActivosItem>
            {
                CrearItem("Usuarios", usuariosActivos, usuariosInactivos),
                CrearItem("Emprendimientos", emprendimientosActivos, emprendimientosInactivos),
                CrearItem("Productos", productosActivos, productosInactivos),
                CrearItem("Pedidos", pedidosActivos, pedidosInactivos),
            }
        };

        return View(model);
    }

    private static RegistrosActivosItem CrearItem(string entidad, int activos, int inactivos)
    {
        var total = activos + inactivos;
        return new RegistrosActivosItem
        {
            Entidad = entidad,
            Activos = activos,
            Inactivos = inactivos,
            Total = total,
            PorcentajeActivos = total > 0 ? Math.Round((double)activos / total * 100, 1) : 0
        };
    }
}
