namespace OtoRehber.Domain.Ai
{
    /// <summary>
    /// AI açıklama katmanının (PRD v5 §4) controller'a döndürdüğü doğrulanmış sonuç.
    /// <see cref="Summary"/> markdown metindir; claim doğrulaması servis içinde yapılır.
    /// </summary>
    public sealed class AiExplanation
    {
        public bool Ok { get; init; }

        /// <summary>Kullanıcıya gösterilecek markdown açıklama (doğrulama sonrası).</summary>
        public string Summary { get; init; } = "";

        public int AcceptedClaims { get; init; }
        public int RejectedClaims { get; init; }

        /// <summary>!Ok ise kullanıcıya gösterilecek hata mesajı.</summary>
        public string? ErrorMessage { get; init; }

        public static AiExplanation Fail(string message) => new() { Ok = false, ErrorMessage = message };
    }
}
