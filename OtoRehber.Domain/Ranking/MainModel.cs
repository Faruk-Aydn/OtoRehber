namespace OtoRehber.Domain.Ranking
{
    /// <summary>
    /// Diversity gruplaması için "ana model" anahtarı (PRD v5 §3.1 hiyerarşisi:
    /// Brand → <b>Main Model</b> → Generation → Engine/Variant).
    ///
    /// Katalog konvansiyonu: <c>ModelName = "&lt;Plaka&gt; (&lt;kasa kodu&gt;, &lt;yıl&gt;)"</c>
    /// (bkz. Data/catalog/README.md). Ana model = ilk "(" öncesi kısım + marka.
    /// Nesil/motor bilgisi anahtara <b>dahil edilmez</b> — farklı nesiller aynı ana modeldir
    /// ama diversity onları birleştirmez, yalnızca sıralı listede sayısını sınırlar.
    /// </summary>
    public static class MainModel
    {
        public static string Key(string? brand, string? modelName)
        {
            var name = modelName ?? "";
            int paren = name.IndexOf('(');
            if (paren >= 0) name = name[..paren];
            name = name.Trim();

            return $"{(brand ?? "").Trim().ToLowerInvariant()}|{name.ToLowerInvariant()}";
        }
    }
}
