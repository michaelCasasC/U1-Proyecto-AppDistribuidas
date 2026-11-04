namespace Proyecto.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, int? userId = null);
    Task<List<EmailLogEntry>> GetRecentEmailsAsync(int count = 50);
}

public class EmailLogEntry
{
    public int Id { get; set; }
    public string Destinatario { get; set; } = "";
    public string Asunto { get; set; } = "";
    public bool Enviado { get; set; }
    public string? Error { get; set; }
    public DateTime FechaEnvio { get; set; }
}
