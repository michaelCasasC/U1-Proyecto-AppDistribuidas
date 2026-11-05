using Microsoft.AspNetCore.Mvc;
using Proyecto.Services;

namespace Proyecto.Controllers;

[Route("Tiempo-Real")]
public class TiempoRealController : Controller
{
    private readonly IUserSessionService _userSession;

    public TiempoRealController(IUserSessionService userSession)
    {
        _userSession = userSession;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpPost("Simular")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> Simular([FromServices] INotificacionService notificacionService)
    {
        var environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!environment.IsDevelopment() && !_userSession.IsLoggedIn)
        {
            return Unauthorized();
        }

        var stock = Random.Shared.Next(1, 25);
        await notificacionService.StockActualizadoAsync(0, "Evento de demostración", stock);
        return Json(new { productoId = 0, nombre = "Evento de demostración", stockNuevo = stock });
    }
}
