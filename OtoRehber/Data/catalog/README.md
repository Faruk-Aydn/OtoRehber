# Araç Kataloğu — JSON Seed / Senkron Sistemi

Bu klasördeki `*.json` dosyaları **doğruluk kaynağıdır**. Uygulama her açılışında
`OtoRehber.Infrastructure/Data/CatalogSeed/CatalogSeeder.cs` ile veritabanını JSON'a
**bildirimsel** olarak senkronlar.

## Senkron mantığı

Anahtar: **`brand` + `modelName` + `engine`** (büyük/küçük harf duyarsız).

| Durum | Sonuç |
|---|---|
| Anahtar JSON'da var, DB'de yok | **Eklenir** (`Source = "catalog"`) |
| Anahtar JSON'da var, DB'de var | **Benimsenir** (`Source = "catalog"`) + tüm alanlar/alt listeler JSON'dan yeniden yazılır |
| `Source = "catalog"`, anahtar JSON'da **yok** | **Budanır (silinir)** — yorumu/garaj kaydı/fiyat geçmişi varsa silinmez, uyarı loglanır |
| `Source = null` (HasData / admin), JSON'da yok | **Dokunulmaz** |

- Katalog satırlarının metinleri her deploy'da JSON'a göre güncellenir; düzenleme
  **JSON dosyasında** yapılır, admin panelde değil.
- Tamamen kapatmak için `Catalog__Sync=false` ortam değişkeni (testlerde kapalı).
- `.json` dosyaları publish çıktısına kopyalanır (csproj `<Content Update>`); Railway deploy'unda otomatik uygulanır.

## Kesin granülerlik kuralları

Her satır **tek bir gerçek varyantı** temsil eder. Birleştirme yasak:

1. **Tek plaka / kayıt.** Asla `A / B`. Golf ≠ Jetta, Clio ≠ Symbol, Fluence ≠ Megane,
   Palio ≠ Albea ≠ Siena, Corsa ≠ Astra, Symbol ≠ Taliant.
2. **Tek nesil / makyaj / kayıt.** Golf 7 ≠ Golf 7.5, Focus Mk2 ≠ Mk2.5,
   Grande Punto ≠ Punto Evo, Astra H ≠ Astra H makyaj.
3. **`modelName` = `<Plaka> (<kasa kodu>, <yıl-yıl>)`** — ör.
   `"Golf (7, 2012-2016)"`, `"Golf (7.5, 2017-2020)"`, `"Corolla (E170, 2013-2016)"`,
   `"Passat (B8, 2014-2023)"`, `"Civic (FD6, 2006-2012)"`, `"Polo (9N, 2002-2005)"`.
   Kasa kodu yoksa `"<Plaka> (<yıl-yıl>)"`.
4. **Tek motor / kayıt.** `1.3 Multijet 75 HP` ve `1.3 Multijet 95 HP` → **iki** kayıt.
   Asla `1.4 8V/16V`, asla `75/90 HP`.
5. **Tek şanzıman / kayıt.** Asla `5/6 İleri Manuel`, asla `... veya Otomatik`,
   asla `Manuel & DSG`.
6. **`engine` = `<hacim> <kod?> <güç> + <şanzıman>`** — ör.
   `"1.6 TDI 115 HP (CRKB) + 7 İleri DSG DQ200"`, `"1.6 Twinport 105 HP + 5 İleri Manuel"`.
7. Türkiye'de gerçekten satılmış her kombinasyonu ekle; satılmamış olanı ekleme.
8. `segment` ∈ `A B C D E SUV MPV Ticari Spor Elektrikli` (geçersiz → seeder `"C"` atar).
9. `severity` ∈ `Düşük | Orta | Kritik` (başka değer → `Orta`).
10. `expertSummary` / `userFeedbackSummary`: hem teknik hem günlük dille, kullanıcı
    dışarıdan araştırmaya ihtiyaç duymayacak kadar detaylı. Kronik arızalar, formlardaki
    genel izlenim, artı/eksi, kilometre bakım barajları eksiksiz. Mekanik olarak aynı
    kasa kardeşlerinde kronik/pros/cons ortak olabilir; giriş cümlesi kasa tipine göre
    (bagaj, aile, ticari) uyarlanır.

## Yapılandırılmış özellikler (filtreleme)

`bodyType` ve `drivetrain` **elle** girilir:
- `bodyType` ∈ `Hatchback | Sedan | Station Wagon | SUV | MPV | Coupe | Cabrio | Pickup | Panelvan`
- `drivetrain` ∈ `Önden Çekiş | Arkadan İtiş | 4WD | AWD`

`fuelType`, `transmission`, `powerHp`, `engineDisplacementCc`, `yearStart`, `yearEnd`, `condition`
**boş bırakılır** — seeder (`CatalogSpecInference`) `engine` + `productionYears` metninden türetir.
Türetme yanlışsa JSON'da açıkça değer vererek geçersiz kıl (`fuelType`, `transmission` ∈ `Manuel|Otomatik`,
`condition` ∈ `İkinci El|Sıfır`). Elektrikli araçlarda `rangeKm` + `fastChargeMinutes` elle verilir.

## Şema

```jsonc
{
  "brand": "Toyota",
  "modelName": "Corolla (E170, 2013-2016)",
  "productionYears": "2013-2016",
  "engine": "1.6 Valvematic 132 HP (1ZR-FAE) + Multidrive S CVT",
  "segment": "C",
  "reliabilityScore": 9.0,          // 1-10
  "minPrice": 860000,                // TL
  "maxPrice": 1250000,               // TL
  "estimatedMaintenanceCostEUR": 260,
  "expertSummary": "…",
  "userFeedbackSummary": "…",
  "bodyType": "Sedan",               // elle
  "drivetrain": "Önden Çekiş",        // elle
  "imageUrl": null,                  // opsiyonel
  // fuelType / transmission / powerHp / engineDisplacementCc / yearStart / yearEnd / condition → seeder türetir
  "chronicIssues": [
    { "title": "…", "description": "…", "severity": "Orta", "estimatedCostEUR": 200, "affectedYears": "2013-2016" }
  ],
  "pros": ["…", "…"],
  "cons": ["…", "…"],
  "milestones": [
    { "mileage": "60.000 km", "expectedIssues": "…", "estimatedCostEUR": 250 }
  ]
}
```

## Dosyalar

`NN-marka.json` — her marka kendi dosyasında. Sıra önemsiz (seeder küme olarak işler);
numaralar sadece okunabilirlik için.
