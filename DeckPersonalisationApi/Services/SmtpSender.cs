using System.Net;
using System.Net.Mail;

namespace DeckPersonalisationApi.Services;

public class SmtpSender
{
    public AppConfiguration Config { get; }

    public SmtpSender(AppConfiguration config)
    {
        Config = config;
    }

    public void Send(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(Config.EmailUser) || string.IsNullOrWhiteSpace(Config.EmailPassword) ||
            string.IsNullOrWhiteSpace(Config.EmailServer))
            throw new Exception("Cannot send email, no credentials");
        
        SmtpClient client = new(Config.EmailServer)
        {
            Port = 587,
            EnableSsl = true,
            Credentials = new NetworkCredential(Config.EmailUser, Config.EmailPassword)
        };

        MailMessage message = new()
        {
            From = new(Config.EmailUser, "DeckThemes"),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
            To = { to }
        };

        client.Send(message);
        Console.WriteLine($"Sent email to {to}");
    }
}