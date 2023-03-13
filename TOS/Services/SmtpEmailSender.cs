using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace TOS.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpClient _client;
    private readonly string _fromAddress;

    public SmtpEmailSender( IConfiguration configuration)
    {
        _fromAddress = configuration["EmailSettings:FromAddress"] ?? throw new Exception("EmailSettings:FromAddress is not set in appsettings.json");
       
        var username = configuration["EmailSettings:Username"] ?? throw new Exception("EmailSettings:Username is not set in appsettings.json");
        var password = configuration["EmailSettings:Password"] ?? throw new Exception("EmailSettings:Password is not set in appsettings.json");
        var credentials = new NetworkCredential(username, password);

        _client = new SmtpClient(configuration["EmailSettings:SmtpServer"])
        {
            Port = int.Parse(configuration["EmailSettings:Port"] ?? throw new Exception("EmailSettings:Port is not set in appsettings.json")),
            Credentials = credentials,
            EnableSsl = bool.Parse(configuration["EmailSettings:EnableSsl"] ?? "true") 
        };
    }
    
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            await _client.SendMailAsync(new MailMessage(_fromAddress, email, subject, htmlMessage));
        }
        catch
        {
            Console.WriteLine("Failed to send email");
        }
    }
}