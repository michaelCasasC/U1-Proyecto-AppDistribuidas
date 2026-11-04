using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Services;
using Proyecto.ViewModels;

namespace Proyecto.Controllers
{
    public class AsistenteController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILocalAiService _localAiService;

        public AsistenteController(AppDbContext context, ILocalAiService localAiService)
        {
            _context = context;
            _localAiService = localAiService;
        }

        public async Task<IActionResult> Index()
        {
            return View(new AiChatViewModel
            {
                AvailableProducts = await GetAvailableProductsAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AiChatViewModel model)
        {
            var products = await GetAvailableProductsAsync();
            model.AvailableProducts = products;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.Answer = await _localAiService.AskForProductRecommendationAsync(model.Question, products);
            }
            catch (HttpRequestException)
            {
                model.ErrorMessage = "No pude conectarme con Ollama. Verifica que esté ejecutándose en http://localhost:11434.";
            }
            catch (TaskCanceledException)
            {
                model.ErrorMessage = "Ollama tardó demasiado en responder. Intenta de nuevo con una pregunta más específica.";
            }

            return View(model);
        }

        private async Task<List<AiProductContextItem>> GetAvailableProductsAsync()
        {
            return await _context.Productos
                .AsNoTracking()
                .Include(producto => producto.Categoria)
                .Include(producto => producto.Emprendimiento)
                .Where(producto =>
                    producto.Activo &&
                    producto.Stock > 0 &&
                    producto.Emprendimiento != null &&
                    producto.Emprendimiento.Activo &&
                    producto.Categoria != null &&
                    producto.Categoria.Activo)
                .OrderBy(producto => producto.Categoria!.Nombre)
                .ThenBy(producto => producto.Nombre)
                .Take(40)
                .Select(producto => new AiProductContextItem
                {
                    Id = producto.Id,
                    Nombre = producto.Nombre,
                    Descripcion = producto.Descripcion,
                    Precio = producto.Precio,
                    Stock = producto.Stock,
                    Categoria = producto.Categoria!.Nombre,
                    Emprendimiento = producto.Emprendimiento!.Nombre
                })
                .ToListAsync();
        }
    }
}
