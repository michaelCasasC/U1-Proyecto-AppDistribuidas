using Microsoft.AspNetCore.Mvc;
using Proyecto.Services;

namespace Proyecto.Controllers;

public class TiempoRealController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Simular([FromServices] INotificacionService notificacionService)
    {
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return NotFound();
        }

        var stock = Random.Shared.Next(1, 25);
        await notificacionService.StockActualizadoAsync(0, "Evento de demostración", stock);
        return Json(new { productoId = 0, nombre = "Evento de demostración", stockNuevo = stock });
    }
}
