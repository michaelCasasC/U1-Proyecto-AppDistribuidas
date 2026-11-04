using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Proyecto.Services
{
    /// <summary>
    /// Envío de correos vía SMTP. La configuración se lee de User Secrets / .env:
    ///   Email:Smtp:Host, Email:Smtp:Port, Email:Smtp:User, Email:Smtp:Password, Email:From
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _configuration["Email:Smtp:Host"]
                       ?? throw new InvalidOperationException("Falta Email:Smtp:Host");
            var port = int.Parse(_configuration["Email:Smtp:Port"] ?? "587");
            var user = _configuration["Email:Smtp:User"]
                       ?? throw new InvalidOperationException("Falta Email:Smtp:User");
            var password = _configuration["Email:Smtp:Password"]
                       ?? throw new InvalidOperationException("Falta Email:Smtp:Password");
            var from = _configuration["Email:From"] ?? user;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(from, from));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            // En algunos entornos la revocación del certificado no se puede
            // verificar por restricciones de red; se desactiva para no fallar.
            client.CheckCertificateRevocation = false;

            // 465 => SSL implícito; 587 (u otros) => STARTTLS.
            var secureOptions = port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(host, port, secureOptions);
            await client.AuthenticateAsync(user, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
