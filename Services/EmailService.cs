using Microsoft.EntityFrameworkCore;
using Proyecto.Data;
using Proyecto.Models;

namespace Proyecto.Services;

public class EmailService : IEmailService
{
    private readonly AppDbContext _context;
    private readonly IEmailSender _emailSender;

    public EmailService(AppDbContext context, IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, int? userId = null)
    {
        var log = new EmailLog
        {
            Destinatario = toEmail,
            Asunto = subject,
            CuerpoHtml = htmlBody,
            UsuarioId = userId,
            FechaEnvio = DateTime.UtcNow
        };

        try
        {
            await _emailSender.SendEmailAsync(toEmail, subject, htmlBody);
            log.Enviado = true;
        }
        catch (Exception ex)
        {
            log.Enviado = false;
            log.Error = ex.Message;
        }

        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<EmailLogEntry>> GetRecentEmailsAsync(int count = 50)
    {
        return await _context.EmailLogs
            .OrderByDescending(e => e.FechaEnvio)
            .Take(count)
            .Select(e => new EmailLogEntry
            {
                Id = e.Id,
                Destinatario = e.Destinatario,
                Asunto = e.Asunto,
                Enviado = e.Enviado,
                Error = e.Error,
                FechaEnvio = e.FechaEnvio
            })
            .ToListAsync();
    }
}
