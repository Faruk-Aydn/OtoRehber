using System.Threading;
using System.Threading.Tasks;

namespace OtoRehber.Domain.Interfaces
{
    /// <summary>
    /// Uygulama e-posta gönderimi (hesap doğrulama, şifre sıfırlama vb.).
    /// </summary>
    public interface IAppEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
