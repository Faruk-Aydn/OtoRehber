# OtoRehber — Proje Rehberi & Canlıya Çıkış Yol Haritası

Bu dosya hem projenin teknik özetini hem de production'a çıkış için adım adım
yol haritasını içerir. Geliştirmeler **Faz 1 → Faz 2 → Faz 3** sırasıyla yapılır.
Her madde tamamlandığında `[ ]` işareti `[x]` yapılır ve "Durum" notu güncellenir.

---

## 1. Proje Özeti

**OtoRehber**, Türkiye ikinci el otomobil piyasası için yapay zeka destekli bir
araç inceleme / karşılaştırma / öneri platformudur.

- Uzman özeti + kullanıcı yorumları + kronik sorunlar + kilometre bakım barajları
- Araç karşılaştırma (AI hakem yorumu ile)
- AI Sihirbaz (profil bazlı araç önerisi) ve AI Chat widget
- YouTube inceleme videolarından (transkript + Gemini) otomatik veri çıkarma (admin)
- Kullanıcı "Garaj"ı (favori araçlar)
- İstatistik paneli, PWA desteği

### Teknoloji
- ASP.NET Core 8 MVC (SDK 9 ile derleniyor, `TargetFramework=net8.0`)
- EF Core 8 + **SQLite** (→ Faz 1'de PostgreSQL'e geçilecek)
- ASP.NET Core Identity (cookie auth, `Admin` rolü)
- AutoMapper, Tailwind (şu an CDN), FontAwesome, AOS, Chart.js
- Google Gemini API (`GeminiApiKey`), YoutubeExplode

### Çözüm yapısı
```
OtoRehber.sln
├─ OtoRehber/                 → Web (MVC, Controllers, Views, Program.cs, Migrations)
├─ OtoRehber.Domain/          → Entities, DTOs, Interfaces, AutoMapper Profile
└─ OtoRehber.Infrastructure/  → DbContext, AiCarDataService
```

### Ana bileşenler
| Alan | Dosya |
|---|---|
| Uygulama girişi / pipeline | `OtoRehber/Program.cs` |
| DbContext + seed | `OtoRehber.Infrastructure/Data/OtoRehberDbContext.cs` |
| AI servisi | `OtoRehber.Infrastructure/Services/AiCarDataService.cs` |
| Admin CRUD + AI import | `OtoRehber/Controllers/AdminCarController.cs` |
| Katalog / arama / leaderboard | `OtoRehber/Controllers/HomeController.cs` |
| Araç detay + yorum | `OtoRehber/Controllers/CarController.cs` |
| Karşılaştırma | `OtoRehber/Controllers/CompareController.cs` |
| Kimlik | `OtoRehber/Controllers/AccountController.cs` |
| Layout / global JS | `OtoRehber/Views/Shared/_Layout.cshtml` |

---

## 2. Geliştirme Komutları

```bash
# Derleme
dotnet build OtoRehber.sln

# Çalıştırma (Development)
dotnet run --project OtoRehber

# Migration ekleme (Migrations assembly = OtoRehber)
dotnet ef migrations add <Ad> --project OtoRehber --startup-project OtoRehber

# Veritabanını güncelleme
dotnet ef database update --project OtoRehber --startup-project OtoRehber

# Production migration bundle
dotnet ef migrations bundle --project OtoRehber --startup-project OtoRehber -o efbundle
```

### Gizli değerler (repoya ASLA yazılmaz)
- `GeminiApiKey`
- `AdminSeed:Email`, `AdminSeed:Password`
- `ConnectionStrings:DefaultConnection`

Development: `dotnet user-secrets`. Production: environment variable.
`appsettings.*.json` (Development/Production/Local) `.gitignore`'da.

---

## 3. Kod Konvansiyonları

- Türkçe UI metinleri ve Türkçe yorum satırları korunur.
- Controller action'ları **async** olmalı (DB erişen her şey `await ...Async()`).
- Kullanıcı girdisi View'da Razor ile otomatik encode edilir; `@Html.Raw` sadece
  kendi ürettiğimiz JSON/serialize veri için, AI/markdown çıktısı için
  `marked.parse` + `DOMPurify.sanitize` zorunlu.
- Yeni tablo/alan → migration + `OtoRehberDbContextModelSnapshot` güncel tutulur.
- Zaman damgaları **`DateTime.UtcNow`** (asla `DateTime.Now`).
- String arama/karşılaştırma **`ToLowerInvariant()`** veya `EF.Functions.Like`
  (Türkçe `I`/`ı` bug'ı).
- Entity string alanlarına `[MaxLength]`, DTO'lara validation attribute'ları.

---

## 4. CANLIYA ÇIKIŞ YOL HARİTASI

### Kararlar (2026-08-27 alındı)
- [x] **Hosting:** Railway / Render (Docker tabanlı, managed Postgres dahil)
- [x] **Production veritabanı:** PostgreSQL. Provider `Database:Provider` config değeri ile seçilir (`Sqlite` | `Postgres`); yerelde SQLite ile geliştirmeye devam.
- [x] **AI erişimi:** Anonim kullanıcıya açık + sıkı IP bazlı rate limit.
- [x] **E-posta:** Resend (aylık 3.000 ücretsiz, basit HTTP API). API key yoksa `IEmailSender` no-op çalışır ve linki log'lar (Faz 1'i bloke etmez). Alan adı doğrulaması sonra.
- [ ] Domain adı

---

## FAZ 1 — Canlıya çıkış blokerleri (BUNLAR BİTMEDEN DEPLOY YOK)

### 1.1 Tehlikeli / geçici endpoint'leri kaldır
- [ ] `AdminCarController.RestoreDatabase` sil (anonim erişim + admin şifre sıfırlama açığı)
- [ ] `AdminCarController.DeleteAllCars` sil veya ayrı korumalı bakım aracına taşı
- [ ] `AdminCarController.DeleteAiCars` sil (`Id > 6` kırılgan mantık)
- [ ] `AdminCarController.AutoFetchImages` sil (Bing scrape → harici URL enjeksiyonu, `new HttpClient()`)
- [ ] Kök dizindeki `fix_migration.py` sil
- **Durum:** _bekliyor_

### 1.2 Gizli anahtar yönetimi
- [ ] `appsettings.json`'dan `GeminiApiKey` ve `AdminSeed:Password` değerleri boş kalsın; koddan `_configuration` ile okunmaya devam
- [ ] `README`/bu dosyada dev için `user-secrets` talimatı
- [ ] Production: environment variable (`GeminiApiKey`, `AdminSeed__Email`, `AdminSeed__Password`, `ConnectionStrings__DefaultConnection`)
- [ ] Admin seed: şifre yoksa kullanıcı **oluşturma** (log'a şifre yazma yok); sadece uyarı logla
- **Durum:** _bekliyor_

### 1.3 SQLite → PostgreSQL
- [ ] `Npgsql.EntityFrameworkCore.PostgreSQL` paketi
- [ ] `Program.cs`: `UseNpgsql` + connection string environment'tan
- [ ] Yerel geliştirme için docker-compose'da postgres servisi
- [ ] Migration'ları temiz üret (`InitialCreate`), `HasData` seed'i koru
- [ ] `.gitignore`'a Postgres volume yolları (gerekirse)
- **Durum:** _bekliyor_ — hosting/DB kararına bağlı. Karar gelene kadar SQLite provider'ı config ile seçilebilir yapılabilir.

### 1.4 AI YouTube import — sahte veri fallback'i kaldır
- [ ] `AiCarDataService.AnalyzeAndSaveFromYoutubeAsync` içindeki hard-coded Corolla transkripti sil
- [ ] Transkript yoksa `null`/anlamlı hata döndür, controller kullanıcıya bildirsin
- [ ] `chunk` döngüsündeki `Task.Delay` → arka plan işine taşınana kadar en azından `IHostedService`/kuyruk notu
- **Durum:** _bekliyor_

### 1.5 CSRF / antiforgery
- [ ] `GarageController.Toggle` → `[ValidateAntiForgeryToken]` + JS'te `RequestVerificationToken` header
- [ ] `AiWizardController.Analyze` → `[ValidateAntiForgeryToken]` + form token
- [ ] `AiChatController.SendMessage` → antiforgery header doğrulaması (cookie auth API)
- [ ] Global: `AutoValidateAntiforgeryTokenAttribute` filtresi (GET hariç tüm POST)
- **Durum:** _bekliyor_

### 1.6 Yorum sistemi güvenliği
- [ ] `CarController.AddReview` → `[Authorize]`
- [ ] `CarReview`'a `UserId` (FK → AppUser) ekle; `UserName` server'da `User` üzerinden set edilsin
- [ ] Aynı kullanıcı aynı araca 1 yorum (unique index) veya düzenleme akışı
- [ ] Yorum endpoint'ine rate limit policy
- [ ] Migration
- **Durum:** _bekliyor_

### 1.7 Kimlik doğrulama sertleştirme
- [ ] Login/Register endpoint'lerine rate limiting policy ("auth")
- [ ] `AccountController.Login` GET → `[AllowAnonymous]` (tutarlılık)
- [ ] E-posta doğrulama: Identity `SignIn.RequireConfirmedAccount` + "ForgotPassword/ResetPassword/ConfirmEmail" sayfaları
- [ ] `IEmailSender` implementasyonu → `ResendEmailSender` (key yoksa no-op + log)
- [ ] Cookie: `SecurePolicy = Always`, `SameSite = Lax`, `HttpOnly`, makul `ExpireTimeSpan`
- [ ] Hesap enumerasyonu: `Register` ve `ForgotPassword` generic mesaj döndürsün
- **Durum:** _bekliyor_

### 1.8 Pipeline / hosting sağlamlaştırma
- [ ] `app.UseForwardedHeaders()` (reverse proxy arkası)
- [ ] DataProtection anahtarlarını kalıcı sakla (`PersistKeysToFileSystem` / Blob / Redis)
- [ ] `AllowedHosts` → gerçek domain
- [ ] `app.UseStatusCodePagesWithReExecute("/Home/Error")` + özel 404/500 View
- [ ] `Program.cs` seeding → `async` (`.Result`/`.Wait()` kaldır)
- [ ] Response compression (`UseResponseCompression`, Brotli)
- [ ] HSTS `max-age=1yıl` + `preload` + `includeSubDomains` (`AddHsts`)
- [ ] `Permissions-Policy` header ekle (geolocation/camera/microphone kapalı)
- **Durum:** _bekliyor_

### 1.9 Hukuki sayfalar (Türkiye / KVKK)
- [ ] KVKK Aydınlatma Metni
- [ ] Kullanım Koşulları
- [ ] Çerez Politikası + çerez onay banner'ı
- [ ] Gizlilik Politikası (gerçek içerik — şu an şablon)
- [ ] Footer'a linkler
- **Durum:** _bekliyor_

### 1.10 Deployment altyapısı
- [ ] `Dockerfile` (multi-stage, `dotnet publish -c Release`)
- [ ] `docker-compose.yml` (app + postgres)
- [ ] `.github/workflows/ci.yml` — build + test
- [ ] `.github/workflows/deploy.yml` — migration bundle + deploy (platforma göre)
- [ ] `/health` health check endpoint
- [ ] `.dockerignore`
- **Durum:** _bekliyor_

---

## FAZ 2 — Kalite, performans, güvenilirlik (çıkıştan hemen sonra)

### 2.1 Async & sorgu optimizasyonu
- [ ] `HomeController.Index`, `CarController.Details`, `CarController.Compare`, `StatsController.Index` → async
- [ ] `HomeController.Index` pagination sınır kontrolü (`page >= 1`, üst sınır)
- [ ] Leaderboard / marka listesi → `IMemoryCache` (5-10 dk)
- [ ] AI context sorgularında `Select` ile kolon daraltma
- [ ] `Car.Brand`, `Car.Segment`, `Car.ReliabilityScore` index

### 2.2 Frontend / asset
- [ ] Tailwind'i build-time derle (CLI veya `Microsoft.AspNetCore.SpaProxy` yerine basit npm script), tek minified CSS
- [ ] FontAwesome / AOS / marked / DOMPurify / Chart.js self-host + bundle + minify
- [ ] `asp-append-version="true"` tüm statik referanslara
- [ ] CDN scriptleri `<script defer>`
- [ ] CSP: `'unsafe-inline'` script kaldır, nonce tabanlı; CDN host'ları temizle

### 2.3 PWA
- [ ] `service-worker.js`: sadece statik asset cache (auth'lu sayfaları cache'leme)
- [ ] `activate` handler + eski cache temizliği + versiyonlama + `skipWaiting`/`clients.claim`
- [ ] Gerçek 192/512/maskable PNG ikonlar + `manifest.json` düzelt
- [ ] PWA offline sayfası (dostça "bağlantı yok" ekranı)
- [ ] `<environment>` tag helper ile dev/prod asset ayrımı

### 2.4 İzleme & loglama
- [ ] Serilog (console + dosya/Seq) veya platform log
- [ ] Sentry (veya benzeri) hata takibi
- [ ] Admin işlemleri için audit log tablosu
- [ ] Uptime monitor (harici)

### 2.5 Veri bütünlüğü
- [ ] `UserGarage (UserId, CarId)` unique index
- [ ] Entity string alanlarına `[MaxLength]` + migration
- [ ] `MinPrice`/`MaxPrice` → `long`
- [ ] Tüm `DateTime.Now` → `DateTime.UtcNow`
- [ ] `ToLower()` → `ToLowerInvariant()` / `EF.Functions.Like`
- [ ] AI karşılaştırma sonuçlarını DB'de cache'le (`car1Id,car2Id` anahtarı)
- [ ] `DbSet<CarPriceHistory>` / `DbSet<ReviewLike>` netleştir, tablo adları tutarlı
- [ ] Migration geçmişini doğrula (`dotnet ef migrations list` + temiz DB'de `database update`)
- [ ] Görsel yüklemede magic-byte kontrolü + EXIF temizleme (re-encode)

### 2.8 AI güvenliği
- [ ] Prompt injection: kullanıcı mesajı / transkript prompt'a girmeden önce sınırla + sistem talimatını ayır
- [ ] AI çıktısı kullanıcıya gösterilmeden önce daima `DOMPurify` (mevcut, kontrol et)

### 2.6 Test
- [ ] `OtoRehber.Tests` projesi (`WebApplicationFactory`)
- [ ] Smoke: ana sayfa, araç detay, login, kayıt, yorum ekleme
- [ ] AI servis için birim test (mock HttpMessageHandler)

### 2.7 Arka plan işleri
- [ ] YouTube import → `IHostedService` / kanal kuyruğu; UI'da ilerleme/polling
- [ ] `Task.Delay` içeren tüm akışları request thread'inden çıkar

---

## FAZ 3 — Ürün geliştirme

### 3.1 Sayfalar
- [ ] Ayrı katalog sayfası (`/araclar`), Home = landing/hero
- [ ] Tam arama sonuçları sayfası
- [ ] Kullanıcı profili / hesap ayarları (şifre, e-posta, hesabı sil)
- [ ] Marka sayfaları (`/marka/{slug}`), segment sayfaları
- [ ] Hakkımızda, İletişim
- [ ] `sitemap.xml`, `robots.txt`
- [ ] Admin: kullanıcı yönetimi, yorum moderasyon kuyruğu, güvenli toplu import UI
- [ ] Blog/haber (opsiyonel, SEO)

### 3.2 Özellikler
- [ ] Kullanıcıya bağlı yorumlar: düzenle/sil, şikayet et, "faydalı" oyu (`ReviewLike` tamamla)
- [ ] Araç kartında ortalama kullanıcı puanı + `AggregateRating` JSON-LD
- [ ] Fiyat geçmişi grafiği (`CarPriceHistory` — veri kaynağı/cron)
- [ ] Garajdaki araçlar için fiyat/haber bildirimi (e-posta)
- [ ] 2+ araç karşılaştırma
- [ ] Araç başına çoklu görsel galerisi
- [ ] Yorumlarda sayfalama + sıralama
- [ ] Gamification (`AppUser.Level`/`XP`) tamamla veya kaldır

### 3.3 SEO / erişilebilirlik
- [ ] Meta description, canonical, Open Graph / Twitter Card (tüm sayfalar)
- [ ] Araç detay: `Car` / `Review` / `AggregateRating` JSON-LD
- [ ] Erişilebilirlik denetimi: alt text, aria, klavye nav (chat + autocomplete), kontrast, focus
- [ ] Gizlilik dostu analytics (onay banner'ına bağlı)

---

## 5. İlerleme Günlüğü

| Tarih | Faz/Madde | Değişiklik | Commit |
|---|---|---|---|
| 2026-08-27 | — | CLAUDE.md yol haritası oluşturuldu | — |
