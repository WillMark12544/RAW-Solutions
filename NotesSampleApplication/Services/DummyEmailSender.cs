using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace NotesSampleApplication.Services
{
    public class DummyEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Just log instead of sending
            Console.WriteLine($"[DummyEmailSender] To: {email}, Subject: {subject}, Message: {htmlMessage}");
            return Task.CompletedTask;
        }
    }
}
