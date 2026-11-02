using System.Net;
using System.Net.Mail;

namespace BTSB.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string recipient, string subject, string htmlBody)
        {
            var host = _configuration["Email:SmtpHost"];
            var portValue = _configuration["Email:SmtpPort"];
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:AppPassword"];
            var senderEmail = _configuration["Email:SenderEmail"];
            var senderName = _configuration["Email:SenderName"] ?? "Buku Tamu Digital BTSB";
            var enableSslValue = _configuration["Email:EnableSsl"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(portValue) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException(
                    "Konfigurasi email belum lengkap. Periksa bagian Email pada konfigurasi aplikasi.");
            }

            if (!int.TryParse(portValue, out var port))
                throw new InvalidOperationException("Email:SmtpPort harus berupa angka.");

            var enableSsl = !bool.TryParse(enableSslValue, out var ssl) || ssl;

            using var message = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(recipient));

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await client.SendMailAsync(message);
        }
    }
}
