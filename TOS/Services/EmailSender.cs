using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace TOS.Services;

/*
  On local must set:
  dotnet user-secrets init
  dotnet user-secrets set "SendinBlue:Username" x
  dotnet user-secrets set "SendinBlue:Password" y
*/

public class EmailSender : IEmailSender
{
    private readonly SmtpClient _client;
    private const string FromAddress = "noreply@tos.tos";
    private const string SmtpServer = "smtp-relay.sendinblue.com";
    private const int Port = 587;

    public EmailSender(SendinBlueSettings settings)
    {
        var credentials = new NetworkCredential(settings.Username, settings.Password);
        
        _client = new SmtpClient(SmtpServer)
        {
            Port = Port,
            Credentials = credentials,
            EnableSsl = true
        };
    }
    
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return _client.SendMailAsync(new MailMessage(FromAddress, email, subject, htmlMessage));
    }
}

//Class for storing SendinBlue credentials from secrets.json
public class SendinBlueSettings
{
    public string Username { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
}