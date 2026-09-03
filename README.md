# OtoRehber

Yapay zeka destekli ikinci el araç inceleme / karşılaştırma / öneri platformu
(ASP.NET Core 8 MVC).

Detaylı mimari ve yol haritası: [CLAUDE.md](CLAUDE.md)

---

## Yerel geliştirme

Gereksinim: .NET 8 SDK.

```bash
# Gizli değerleri user-secrets ile ver (repoya yazılmaz)
cd OtoRehber
dotnet user-secrets set "GeminiApiKey" "AIza..."
dotnet user-secrets set "AdminSeed:Password" "GucluBirSifre1!"
dotnet user-secrets set "Resend:ApiKey" "re_..."      # opsiyonel; yoksa e-postalar log'a yazılır

# Çalıştır (yerelde SQLite kullanır — Database:Provider=Sqlite varsayılan)
cd ..
dotnet run --project OtoRehber
```

> **Not:** Yerel SQLite şeması `EnsureCreated` ile oluşturulur (migration kullanmaz).
> Modeli (entity) değiştirdiyseniz `OtoRehber/OtoRehberDB.db*` dosyalarını silip
> yeniden çalıştırın.

### PostgreSQL ile yerel çalıştırma (production'a en yakın)

```bash
docker compose up --build
# uygulama:  http://localhost:8080
# health:    http://localhost:8080/health
```

---

## Production'a çıkış (Railway / Render)

1. GitHub reposunu Railway/Render'a bağlayın. Platform kök dizindeki `Dockerfile`'ı
   otomatik algılar.
2. Projeye bir **PostgreSQL** eklentisi ekleyin.
3. Aşağıdaki ortam değişkenlerini tanımlayın:

| Değişken | Açıklama |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Database__Provider` | `Postgres` |
| `ConnectionStrings__DefaultConnection` | Postgres bağlantı dizesi (`Host=...;Database=...;Username=...;Password=...`) |
| `DataProtection__KeyPath` | Kalıcı bir dizin, örn. `/data/keys` (kalıcı disk/volume bağlayın) |
| `AdminSeed__Email` | İlk admin e-postası |
| `AdminSeed__Password` | İlk admin şifresi (verilmezse admin oluşturulmaz) |
| `GeminiApiKey` | Google Gemini API anahtarı |
| `Resend__ApiKey` | Resend API anahtarı (e-posta doğrulama/şifre sıfırlama için) |
| `Resend__FromEmail` | Gönderen, örn. `OtoRehber <no-reply@alanadiniz.com>` |
| `AllowedHosts` | Alan adınız, örn. `otorehber.com` |
| `Currency__EurToTry` | (Opsiyonel) Bakım maliyetlerini ₺ göstermek için € kuru, örn. `48.5`. Boşsa € gösterilir. |
| `Currency__RateDate` | (Opsiyonel) Kur tarihi, örn. `4 Eylül 2026`. |

4. İlk dağıtımda Postgres şeması `context.Database.Migrate()` ile otomatik oluşturulur.
5. Kalıcı disk (`/data/keys`) bağlamazsanız her yeniden başlatmada tüm kullanıcılar
   çıkış yapar ve form gönderimleri (antiforgery) bozulur.

### Yeni migration üretme (şema değişince)

```bash
dotnet ef migrations add <Ad> --project OtoRehber --startup-project OtoRehber
# tasarım zamanı her zaman PostgreSQL kullanır (OtoRehberDbContextFactory)
```

---

## Deploy öncesi kontrol listesi

- [ ] `AdminSeed__Password`, `GeminiApiKey`, `Resend__ApiKey` env olarak verildi
- [ ] Postgres bağlantısı + kalıcı `DataProtection` dizini ayarlandı
- [ ] `AllowedHosts` gerçek alan adına sabitlendi
- [ ] Hukuki sayfalardaki `[Şirket Unvanı]`, `[adres]`, `[e-posta]` alanları dolduruldu
- [ ] Resend'de gönderen alan adı doğrulandı (yoksa e-postalar spam'e düşebilir)
