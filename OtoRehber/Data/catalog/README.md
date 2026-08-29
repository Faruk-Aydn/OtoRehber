# Araç Kataloğu — JSON Seed Sistemi

Bu klasördeki `*.json` dosyaları, uygulama her açılışında **idempotent** olarak veritabanına
işlenir (`OtoRehber.Infrastructure/Data/CatalogSeed/CatalogSeeder.cs`).

## Nasıl çalışır

- Anahtar: **`brand` + `modelName` + `engine`** (büyük/küçük harf duyarsız).
- Kayıt **yoksa** → alt listeleriyle (kronik arıza / artı / eksi / km barajı) birlikte **eklenir**.
- Kayıt **varsa** → **dokunulmaz** (admin panelden yapılan düzenlemeler korunur).
- `Catalog__ForceUpdate=true` ortam değişkeni ile açılırsa → mevcut kayıtların **tüm alanları ve
  alt listeleri katalogdan yeniden yazılır** (admin düzenlemelerini ezer). İçerik güncellemesi
  yayınlarken kullan, sonra değişkeni kaldır.
- Dosyalar `PreserveNewest` ile publish çıktısına kopyalanır; Railway deploy'unda otomatik uygulanır.

## Yeni araç eklerken

1. Yeni bir dosya oluştur (`10-fiat.json`, `20-renault.json` gibi) veya mevcut dosyaya ekle.
2. Her **motor + şanzıman kombinasyonu ayrı bir kayıt** olur.
3. `modelName`: model + nesil/kasa kodu + yıl aralığı — ör. `"Golf 7 / Golf 7.5 (2012-2020)"`.
4. `engine`: hacim + kod + güç + şanzıman — ör. `"1.6 TDI 115 HP (CRKB) + 7 İleri DSG DQ200"`.
5. `segment`: yalnızca `OtoRehber.Domain.CarSegments.All` değerlerinden biri
   (`A B C D E SUV MPV Ticari Spor Elektrikli`) — geçersizse `"C"` atanır.
6. `severity`: `Düşük` | `Orta` | `Kritik` (başka değer → `Orta`).
7. `expertSummary` / `userFeedbackSummary`: hem teknik hem günlük dille, kullanıcı dışarıdan
   araştırmaya ihtiyaç duymayacak kadar detaylı yaz. Kronik arızalar, formlardaki genel izlenim,
   artı/eksi, bakım barajları eksiksiz olsun.

## Şema

```jsonc
{
  "brand": "Toyota",
  "modelName": "Corolla (E170/E180, 2013-2019)",
  "productionYears": "2013-2019",
  "engine": "1.6 Valvematic 132 HP (1ZR-FAE) + Multidrive S CVT",
  "segment": "C",
  "reliabilityScore": 9.0,          // 1-10
  "minPrice": 860000,                // TL
  "maxPrice": 1350000,               // TL
  "estimatedMaintenanceCostEUR": 260,
  "expertSummary": "…",
  "userFeedbackSummary": "…",
  "imageUrl": null,                  // opsiyonel
  "chronicIssues": [
    { "title": "…", "description": "…", "severity": "Orta", "estimatedCostEUR": 200, "affectedYears": "2013-2019" }
  ],
  "pros": ["…", "…"],
  "cons": ["…", "…"],
  "milestones": [
    { "mileage": "60.000 km", "expectedIssues": "…", "estimatedCostEUR": 250 }
  ]
}
```

## Mevcut batch'ler

| Dosya | Kapsam |
|---|---|
| `00-amiral-batch.json` | En çok aranan 25 varyant (Corolla E120/E140/E170, Civic FD6/FB7, Golf 5-7, Megane 2 / Fluence, Clio 4, Egea, Focus 2/3, Astra H/J, Passat B7, BMW 320d E90, Mercedes C180 W204) |
