using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Proyecto.ViewModels;

namespace Proyecto.Services
{
    public class OllamaService : ILocalAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OllamaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> AskForProductRecommendationAsync(string question, IReadOnlyCollection<AiProductContextItem> products)
        {
            var model = _configuration["Ollama:Model"] ?? "qwen2.5:0.5b";
            var catalogContext = BuildCatalogContext(products);

            var request = new OllamaChatRequest
            {
                Model = model,
                Stream = false,
                Messages = new List<OllamaMessage>
                {
                    new()
                    {
                        Role = "system",
                        Content = """
                        Eres el asistente local de EmprendeMarket.
                        Responde en español, de forma breve y útil.
                        Recomienda únicamente productos presentes en el catálogo disponible.
                        Si el usuario pregunta por algo que no existe en el catálogo, sugiere alternativas parecidas disponibles.
                        Incluye nombre del producto, precio y emprendimiento cuando recomiendes algo.
                        No inventes stock, precios, enlaces ni productos.
                        """
                    },
                    new()
                    {
                        Role = "user",
                        Content = $"Catálogo disponible:\n{catalogContext}\n\nPregunta del usuario: {question}"
                    }
                }
            };

            using var response = await _httpClient.PostAsJsonAsync("/api/chat", request);
            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
            var answer = ollamaResponse?.Message?.Content?.Trim();

            return string.IsNullOrWhiteSpace(answer)
                ? "No pude generar una recomendación en este momento."
                : answer;
        }

        private static string BuildCatalogContext(IReadOnlyCollection<AiProductContextItem> products)
        {
            if (products.Count == 0)
            {
                return "No hay productos disponibles actualmente.";
            }

            var builder = new StringBuilder();
            foreach (var product in products)
            {
                builder.Append("- ");
                builder.Append(product.Nombre);
                builder.Append(" | Precio: $");
                builder.Append(product.Precio.ToString("0.00"));
                builder.Append(" | Stock: ");
                builder.Append(product.Stock);
                builder.Append(" | Categoría: ");
                builder.Append(product.Categoria);
                builder.Append(" | Emprendimiento: ");
                builder.Append(product.Emprendimiento);

                if (!string.IsNullOrWhiteSpace(product.Descripcion))
                {
                    builder.Append(" | Descripción: ");
                    builder.Append(product.Descripcion);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private sealed class OllamaChatRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public List<OllamaMessage> Messages { get; set; } = new();

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }
        }

        private sealed class OllamaMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        private sealed class OllamaChatResponse
        {
            [JsonPropertyName("message")]
            public OllamaMessage? Message { get; set; }
        }
    }
}
