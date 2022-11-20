using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace TOS.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpClient _client;
    private readonly string _fromAddress;

    public SmtpEmailSender(SmtpEmailSenderSettings settings)
    {
        _fromAddress = settings.FromAddress;
        var credentials = new NetworkCredential(settings.Username, settings.Password);
        
        _client = new SmtpClient(settings.SmtpServer)
        {
            Port = settings.Port,
            Credentials = credentials,
            EnableSsl = settings.EnableSsl
        };
    }
    
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return _client.SendMailAsync(new MailMessage(_fromAddress, email, subject, htmlMessage));
    }
}

public class SmtpEmailSenderSettings
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
}