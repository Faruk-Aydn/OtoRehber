namespace OtoRehber.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        /// <summary>HTTP durum kodu (404, 500 vb.). 0 = bilinmiyor / genel hata.</summary>
        public int StatusCode { get; set; }
    }
}
