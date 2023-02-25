﻿using System.Net;
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
    
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        await _client.SendMailAsync(new MailMessage(_fromAddress, "martinik.filip01@gmail.com", subject, htmlMessage));
    }
}

public class SmtpEmailSenderSettings
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string SmtpServer { get; init; } = string.Empty;
    public int Port { get; init; }
    public bool EnableSsl { get; init; }
}