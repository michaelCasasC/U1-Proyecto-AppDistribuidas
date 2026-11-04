using Proyecto.ViewModels;

namespace Proyecto.Services
{
    public interface ILocalAiService
    {
        Task<string> AskForProductRecommendationAsync(string question, IReadOnlyCollection<AiProductContextItem> products);
    }
}
