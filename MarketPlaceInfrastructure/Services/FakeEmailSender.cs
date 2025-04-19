using Microsoft.AspNetCore.Identity.UI.Services;

public class FakeEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Console.WriteLine($"Fake send email to {email} with subject '{subject}'");
        return Task.CompletedTask;
    }
}