using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SmartTender.Infrastructure.Email;

public class EmailSender
{
    private readonly EmailSettings _settings;

    public EmailSender(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = body };
        message.Body = builder.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);  // burda dəyişiklik
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            throw new Exception("Email göndərmə xətası: " + ex.Message);
        }
    }
}