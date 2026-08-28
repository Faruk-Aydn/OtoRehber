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
- Tailwind (build-time), FontAwesome, AOS, Chart.js (hepsi self-host); Car↔DTO dönüşümü elle (`CarMappings`)
- Google Gemini API (`GeminiApiKey`), YoutubeExplode

### Çözüm yapısı
```
OtoRehber.sln
├─ OtoRehber/                 → Web (MVC, Controllers, Views, Program.cs, Migrations)
├─ OtoRehber.Domain/          → Entities, DTOs, Interfaces, Mappings (CarMappings)
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

# Testler (xUnit + WebApplicationFactory)
dotnet test OtoRehber.sln

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

### 1.1 Tehlikeli / geçici endpoint'leri kaldır ✅
- [x] `AdminCarController.RestoreDatabase` silindi
- [x] `AdminCarController.DeleteAllCars` silindi
- [x] `AdminCarController.DeleteAiCars` silindi
- [x] `AdminCarController.AutoFetchImages` silindi
- [x] `fix_migration.py` silindi
- [x] `Views/AdminCar/Index` ilgili butonlar kaldırıldı
- **Durum:** _tamam (commit 1)_

### 1.2 Gizli anahtar yönetimi ✅
- [x] `appsettings.json` gizli alanları boş; kod `_configuration` ile okuyor
- [x] `csproj` `<UserSecretsId>` eklendi (dev `dotnet user-secrets`)
- [x] Prod env değişkenleri: `README.md` + `docker-compose.yml`'de belgelendi
- [x] Admin seed: şifre yoksa kullanıcı oluşturulmuyor (log'a şifre yazılmıyor)
- **Durum:** _tamam (commit 2)_

### 1.3 SQLite → PostgreSQL ✅
- [x] `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.10
- [x] `Database:Provider` config anahtarı (Sqlite=EnsureCreated, Postgres=Migrate)
- [x] `docker-compose.yml` postgres servisi
- [x] Migration'lar PostgreSQL'e göre yeniden üretildi + `OtoRehberDbContextFactory`
- **Durum:** _tamam (commit 2)_. Not: yerel Sqlite `EnsureCreated` kullandığı için model değişince `OtoRehberDB.db` silinmeli.

### 1.4 AI YouTube import — sahte veri fallback'i kaldır ✅
- [x] Hard-coded Corolla transkripti kaldırıldı
- [x] Transkript yoksa `InvalidOperationException`; `ImportFromYoutube` yakalayıp kullanıcıya gösteriyor
- [ ] `Task.Delay` hâlâ istek içinde (admin-only) → Faz 2.7
- **Durum:** _tamam (commit 1)_

### 1.5 CSRF / antiforgery ✅
- [x] Global `AutoValidateAntiforgeryTokenAttribute`
- [x] `AddAntiforgery` header `RequestVerificationToken`
- [x] `_Layout` global token + `postJson` helper; garaj toggle / AI chat güncellendi
- [x] Test: token'sız POST → 400, token'lı → 200 (doğrulandı)
- **Durum:** _tamam (commit 3)_

### 1.6 Yorum sistemi güvenliği ✅
- [x] `CarController.AddReview` → `[Authorize]` + `[EnableRateLimiting("review")]` + async
- [x] `CarReview.UserId` (FK → AppUser); `UserName` sunucuda e-posta yerel kısmından
- [x] `(CarId, UserId)` unique index; `UserGarage (UserId, CarId)` unique index
- [x] Migration + `Details.cshtml` formu login'e bağlandı
- **Durum:** _tamam (commit 3)_

### 1.7 Kimlik doğrulama sertleştirme ✅
- [x] `auth` rate limit → Login/Register/ForgotPassword/ResetPassword
- [x] `Login` GET `[AllowAnonymous]` (controller seviyesinde)
- [x] `SignIn.RequireConfirmedAccount` + ConfirmEmail/ForgotPassword/ResetPassword akışları + view'lar
- [x] `ResendEmailSender` (key yoksa no-op + log)
- [x] Cookie: HttpOnly + Secure(Always) + SameSite=Lax + 7 gün sliding
- [x] Hesap enumerasyonu: Register/ForgotPassword generic yanıt
- [x] Test: kayıt→doğrulama linki→onay→giriş; doğrulanmamış giriş engelli (doğrulandı)
- **Durum:** _tamam (commit 3)_

### 1.8 Pipeline / hosting sağlamlaştırma ✅
- [x] `UseForwardedHeaders`
- [x] DataProtection `PersistKeysToFileSystem` (`DataProtection:KeyPath`)
- [x] `UseStatusCodePagesWithReExecute` + özel Türkçe `Error.cshtml` (404/403/429/500)
- [x] Seeding `async` (`.Result`/`.Wait()` kaldırıldı)
- [x] `UseResponseCompression` (Brotli + Gzip)
- [x] `AddHsts` 1 yıl + preload + includeSubDomains
- [x] `Permissions-Policy` header
- [x] `/health` health check (`AddDbContextCheck`)
- [ ] `AllowedHosts` → prod'da env ile domain (deploy anında)
- **Durum:** _tamam (commit 3), AllowedHosts deploy'da_

### 1.9 Hukuki sayfalar (Türkiye / KVKK) ✅
- [x] KVKK Aydınlatma Metni, Kullanım Koşulları, Çerez Politikası, Hakkımızda, İletişim
- [x] Gizlilik Politikası gerçek içerikle yeniden yazıldı
- [x] Footer linkleri + çerez onay banner'ı (`localStorage`)
- [ ] Köşeli parantezli işletme bilgileri (`[Şirket Unvanı]`, `[adres]`, `[e-posta]`) doldurulmalı
- **Durum:** _tamam (commit 4), placeholder'lar doldurulacak_

### 1.10 Deployment altyapısı ✅
- [x] `Dockerfile` (multi-stage, non-root, HEALTHCHECK)
- [x] `.dockerignore`
- [x] `docker-compose.yml` (web + postgres + volumes)
- [x] `.github/workflows/ci.yml` (restore/build/test/docker build)
- [x] `/health` endpoint
- [x] `README.md` — Railway deploy adımları
- [ ] `deploy.yml` — Railway otomatik build kullanıyor; ayrı workflow opsiyonel
- **Durum:** _tamam (commit 4)_

---

## FAZ 2 — Kalite, performans, güvenilirlik (çıkıştan hemen sonra)

### 2.1 Async & sorgu optimizasyonu ✅
- [x] `HomeController.Index`, `CarController.Details`, `CarController.Compare`, `StatsController.Index` → async (+ `AsNoTracking`, `AsSplitQuery`)
- [x] `HomeController.Index` pagination sınır kontrolü (`Math.Clamp`)
- [x] Leaderboard / marka listesi → `IMemoryCache` (5 dk); admin CRUD + yorumda invalidate
- [x] AI context sorgularında `Select` ile kolon daraltma (6 kolon)
- [x] `Car.Brand`, `Car.Segment`, `Car.ReliabilityScore` index

### 2.2 Frontend / asset ✅ (kısmen)
- [x] Tailwind build-time derleme — npm `build:css` script (`tailwind.config.js` + `app.src.css` → `wwwroot/css/app.min.css` commit'lenir, Docker Node istemez). CDN JIT kaldırıldı.
- [x] FontAwesome (css+webfonts) / AOS / marked / DOMPurify / Chart.js / DataTables + jQuery(admin) → `wwwroot/lib/` altında self-host
- [x] `asp-append-version="true"` self-host edilen tüm referanslara
- [x] CSP: tüm CDN host'ları kaldırıldı → `script-src`/`style-src` sadece `'self' 'unsafe-inline'`, `font-src 'self' data:`
- [ ] `'unsafe-inline'` script → nonce tabanlı (inline script çok, ayrı iş)
- [ ] CDN/self-host scriptlerine `<script defer>` + `<environment>` dev/prod ayrımı

### 2.R Responsive (mobil / tablet / masaüstü) ✅
- [x] Navbar hamburger menü (lg altında); mobil arama + tema toggle
- [x] `html/body { overflow-x: clip }`, `img/table { max-width: 100% }`
- [x] Home hero/arama formu breakpoint'leri; AOS yatay slide'lar `fade-up`'a
- [x] Stats marka tablosu mobilde kolon gizleme; toast/AI-chat mobil boyut
- [x] Puppeteer ile 390/768/1280 px doğrulama — yatay taşma yok (prod dahil)

### 2.3 PWA ✅
- [x] `service-worker.js` yeniden yazıldı: sadece statik asset cache (`/css`, `/js`, `/lib`, `/icons`, `/images` + statik uzantılar) — gezinme/HTML yanıtları **asla** cache'lenmez (network-first, hata → `offline.html`)
- [x] `activate` handler: eski cache sürümlerini sil + `CACHE_VERSION` (`v3`) + `skipWaiting()`/`clients.claim()`
- [x] Gerçek PNG ikonlar (`wwwroot/icons/`): `icon-192`, `icon-512`, `icon-maskable-512` (mavi zemin + beyaz araç, maskable safe-zone padding'li)
- [x] `manifest.json` düzeltildi: `id`/`scope`/`lang`/`categories` + 3 gerçek PNG ikon (any + maskable)
- [x] `wwwroot/offline.html` — self-contained (dış referans yok), tema-duyarlı, "Yeniden dene" butonu
- [x] `_Layout` `<head>`: `apple-touch-icon` + `apple-mobile-web-app-*` meta, `icon` 192 PNG
- [ ] `<environment>` tag helper dev/prod ayrımı → tüm asset'ler self-host olduğu için düşük öncelik (Faz 2 devam)

### 2.4 İzleme & loglama ✅ (kısmen)
- [x] Serilog — yapılandırılmış console log (`UseSerilog` + `UseSerilogRequestLogging`); Railway/Render yakalar. EF SQL logları `Warning`'e çekildi. (commit `6024435`)
- [x] Admin işlemleri için audit log tablosu — `AuditLog` entity + `AuditLogs` DbSet + migration `AddAuditLog`; `AdminCarController` Create/Update/Delete/Import sonrası kayıt (UserId/UserName/IP)
- [ ] Sentry (veya benzeri) hata takibi — DSN gerekir, opsiyonel
- [ ] Uptime monitor (harici) — kod işi değil, kullanıcı kurar (UptimeRobot vb.)

### 2.5 Veri bütünlüğü ✅ (kısmen)
- [x] `UserGarage (UserId, CarId)` unique index (Faz 1.6)
- [x] Entity kısa string alanlarına `[MaxLength]` + migration (`Faz2DataIntegrity`); uzun prose alanları `text`
- [x] `MinPrice`/`MaxPrice` → `long` (entity + DTO'lar)
- [x] Tüm `DateTime.Now` → `DateTime.UtcNow` (Faz 1'de temizlendi)
- [x] `ToLower()` → `ToLowerInvariant()` (Home/Search/Admin)
- [x] AI karşılaştırma sonuçları `IMemoryCache`'te (`compare-verdict:{a}-{b}`, 6 saat); hata/kota mesajları cache'lenmez (uzunluk < 150). (DB yerine bellek — araç verisi nadir değişir, sonuç yumuşak metin)
- [ ] `DbSet<CarPriceHistory>` / `DbSet<ReviewLike>` netleştir → Faz 3 (özellik gelince)
- [x] Görsel yüklemede magic-byte kontrolü (EXIF re-encode → Faz 3, ImageSharp)

### 2.8 AI güvenliği ✅
- [x] Prompt injection: kullanıcı girdisi 2000/200 kr sınır + sistem talimatı ayrımı + "talimat gaspını yok say" kuralı
- [x] AI çıktısı: Compare/AiWizard Result'ta `DOMPurify.sanitize(marked.parse())`; _Layout chat escape-first
- [x] `GeminiModel` config anahtarı; varsayılan **`gemini-3.5-flash-lite`** (ücretsiz katmanda 15 RPM / 500 RPD). `gemini-2.0-flash` artık 404 veriyor.

### 2.6 Test ✅
- [x] `OtoRehber.Tests` projesi (xUnit + `WebApplicationFactory<Program>`; `Program.cs`'e `public partial class Program`)
- [x] `CustomWebApplicationFactory`: her örneğe özel geçici SQLite dosyası, secrets boş
- [x] Smoke (`SmokeTests`): `/`, `/Stats`, `/Compare`, `/Account/Login|Register|ForgotPassword`, `/Home/Kvkk|Privacy`, `/health`, `/health/ready`, `/manifest.json`, `/service-worker.js`, `/offline.html`, `/Car/Details/1` → 200; `/AdminCar` anon → login'e redirect; token'sız POST → 400
- [x] `AiCarDataServiceTests`: mock `HttpMessageHandler` — key yok / başarı / 429 senaryoları
- [x] `CarMappingsTests`: `ToListDto`/`ToDetailDto`/`ToEntity`/`ApplyTo` (AutoMapper kaldırıldıktan sonra)
- [x] `ci.yml`'den `continue-on-error` kaldırıldı (test artık zorunlu)
- **23 test, hepsi geçiyor.**

### 2.7 Arka plan işleri ✅
- [x] YouTube import → `Channel<Guid>` kuyruğu (`YoutubeImportQueue`) + `YoutubeImportHostedService : BackgroundService`; bellek içi `ImportJobStatus` deposu (admin-only, tek instance)
- [x] `AdminCarController.ImportFromYoutube` POST artık işi kuyruğa atıp `ImportStatus/{id}`'ye redirect ediyor (~150 ms); `ImportStatus.cshtml` 2 sn polling ile durum gösterir
- [x] `Task.Delay(6000)` artık BackgroundService thread'inde, request'te değil
- [x] Audit log + Home cache invalidation başarıda hosted service içinde
- [x] Bonus: `AiCarDataService` altyazı seçimi düzeltildi (`GetByLanguage` bulamayınca exception fırlatıyordu → `Tracks.FirstOrDefault` ile tr→tr*→ilk)

---

## FAZ 3 — Ürün geliştirme

### 3.1 Sayfalar
- [ ] Ayrı katalog sayfası (`/araclar`), Home = landing/hero
- [ ] Tam arama sonuçları sayfası
- [x] Kullanıcı hesap ayarları — `ManageController` (`/Manage`): profil özeti (e-posta + yorum/garaj sayısı),
  şifre değiştir (`RefreshSignIn`), e-posta değiştir (yeni adrese doğrulama linki + `ConfirmEmailChange`),
  **hesabı sil** (şifre + onay kutusu; yorumlar cascade, garaj kayıtları elle silinir — KVKK silme hakkı).
  Navbar'a "Hesabım" linki. `/Manage` → `noindex` + robots.txt disallow.
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

### 3.3 SEO / erişilebilirlik ✅ (kısmen)
- [x] `_Layout` `<head>`: meta description, canonical, Open Graph + Twitter Card (`summary_large_image`), `og:image` (varsayılan `icon-512`, araç sayfasında araç görseli), `robots` (Account/AdminCar/Garage → `noindex`)
- [x] Sayfa bazlı `ViewData["Description"]` — Home/Compare/Stats/AiWizard/Car Details
- [x] `SeoController` → dinamik `/robots.txt` + `/sitemap.xml` (statik sayfalar + tüm `Car/Details/{id}`; carId listesi 1 saat cache)
- [x] Araç detay: `Car` JSON-LD (`AggregateOffer` fiyat aralığı + yorum varsa `AggregateRating`)
- [ ] Erişilebilirlik denetimi: alt text, aria, klavye nav (chat + autocomplete), kontrast, focus
- [ ] Gizlilik dostu analytics (onay banner'ına bağlı)
- [ ] Blog/haber + marka/segment landing sayfaları (SEO içerik derinliği)

---

## 5. İlerleme Günlüğü

| Tarih | Faz/Madde | Değişiklik | Commit |
|---|---|---|---|
| 2026-08-27 | — | CLAUDE.md yol haritası oluşturuldu | — |
| 2026-08-27 | 1.1, 1.4 | Tehlikeli endpoint'ler + AI sahte-veri fallback kaldırıldı | f2b020e |
| 2026-08-27 | 1.2, 1.3 | PostgreSQL desteği + gizli anahtar sertleştirme | e1fde7f |
| 2026-08-27 | 1.5–1.8 | CSRF, e-posta doğrulama, şifre sıfırlama, pipeline sertleştirme | aeba250 |
| 2026-08-27 | 1.9, 1.10 | KVKK/hukuki sayfalar + çerez banner + Docker/CI + README | 12cc109 |
| 2026-08-27 | 2.1 | Controller'lar async + pagination sınır + IMemoryCache + AI context daraltma | b30b262, a897658 |
| 2026-08-27 | 2.5 | Entity `[MaxLength]` + Car index'leri + MinPrice/MaxPrice `long` + görsel magic-byte | a897658 |
| 2026-08-27 | 2.8 | AI prompt injection çerçevesi + `GeminiModel` config (model adı düzeltme) | a897658 |
| 2026-08-27 | 2.4 | Serilog + admin audit log tablosu | 6024435 |
| 2026-08-27 | responsive | Navbar hamburger + taşma düzeltmeleri (mobil/tablet/masaüstü) | 3529647 |
| 2026-08-27 | 2.2 | Tailwind build-time derleme (CDN JIT kaldırıldı) | 24947e5 |
| 2026-08-28 | 2.2 | `app.min.css` FontAwesome sonrası yüklenir (`.hidden` çakışması) + `-c` bayrağı | 0c164fb |
| 2026-08-28 | 2.2 | FontAwesome/AOS/marked/DOMPurify/Chart.js/DataTables self-host + CSP'den CDN kaldırıldı | 5e16da0 |
| 2026-08-28 | 2.3 | PWA: service-worker yeniden yazıldı (statik-only, offline.html) + gerçek PNG ikonlar + manifest düzeltme | c407783 |
| 2026-08-28 | 2.7 | YouTube import → `Channel` kuyruğu + `BackgroundService` + polling durum sayfası; `Task.Delay` request'ten çıktı | 533731e |
| 2026-08-28 | 2.6 | `OtoRehber.Tests` (xUnit + `WebApplicationFactory`): 19 smoke + AI birim testi; CI'da `continue-on-error` kaldırıldı | e7f8198 |
| 2026-08-28 | güvenlik | AutoMapper kaldırıldı (elle `CarMappings`) + savunmasız paketler yükseltildi (AngleSharp/STJ/Caching.Memory/EF Core) + jQuery validation fix | 8e261d2 |
| 2026-08-28 | 2.5 | AI karşılaştırma sonucu `IMemoryCache` (6 saat, `compare-verdict:{a}-{b}`) — tekrar eden Gemini çağrısı yok | cdab798 |
| 2026-08-28 | 3.3 | SEO: `_Layout` meta/OG/Twitter/canonical + `SeoController` (robots.txt + sitemap.xml) + araç detay `Car` JSON-LD; auth sayfaları `noindex` | 9fd8d8d |
| 2026-08-28 | pipeline | DataProtection anahtarları PostgreSQL'e (`PersistKeysToDbContext`, migration `AddDataProtectionKeys`); test izolasyonu düzeltildi | cc4dfb1 |
| 2026-08-28 | fix | Gemini varsayılan modeli `gemini-3.5-flash-lite` (`gemini-2.0-flash` 404) + Compare tek `AsSplitQuery` | acaed39, 2d98162 |
| 2026-08-28 | 3.1 | Kullanıcı hesap ayarları (`ManageController`): şifre/e-posta değiştir + hesabı sil (KVKK) | f55a639 |
| 2026-08-27 | 1.3, 1.10 | Railway `DATABASE_URL` ayrıştırma + Docker healthcheck düzeltme + deploy rehberi | 93c5dbb |
| 2026-08-27 | deploy | Postgres bağlantı çözümü + `railway.json` | 1e7fb51 |
| 2026-08-27 | deploy | `PORT` env dinleme + healthcheck timeout | 1c974f8 |
| 2026-08-27 | deploy | DataProtection anahtarları PostgreSQL'de, `.env` deseni (sonradan geri alındı) | 11fef36 |
| 2026-08-27 | deploy | `/health` saf liveness, `/health/ready` DB kontrolü | 306fadc |
| 2026-08-27 | **deploy — KÖK SORUN** | `.NET 10` paket sızması (`Microsoft.Extensions.Identity.Stores` 10.0.10) → DataProtection crypto uyumsuzluğu → tüm POST 400. `FrameworkReference` ile çözüldü | 9c6ea2b |
| 2026-08-27 | deploy | Geçici teşhis kodu + debug logları temizlendi | a258301 |

**Faz 1 tamamlandı ve CANLIDA:** https://otorehber-production.up.railway.app

### Deploy sırasında öğrenilenler (Faz 2 / gelecek için)
- **Sınıf kütüphaneleri** (`OtoRehber.Domain`, `.Infrastructure`) ASP.NET Core API'si
  kullanıyorsa `<FrameworkReference Include="Microsoft.AspNetCore.App" />` şart —
  `Microsoft.AspNetCore.*` paketlerini NuGet'ten sürüm belirterek çekme (runtime
  ile uyumsuz kopya uygulamaya girer, DataProtection çöker).
- Paket sürümleri **daima** `net8.0` ile aynı major (10.0.x paketi = felaket).
- Railway: `PORT` env'e bağlan, `AllowedHosts=*`, volume kullanma (root-owned),
  `DATABASE_URL` = `${{Postgres.DATABASE_URL}}`.
- DataProtection anahtarları artık PostgreSQL'de (`DataProtectionKeys` tablosu, `PersistKeysToDbContext`) — deploy'da oturumlar düşmez.

### Kalan küçük işler
- Railway `AllowedHosts` → gerçek domain'e sabitle (şu an `*`; healthcheck'i kırmadan)
- Gerçek `GeminiApiKey` (`AIza...`), Resend domain doğrulaması

### Faz 2 — YAPILAN (bu oturuma kadar, canlıda test edilecek)
- 2.1 async + IMemoryCache + AI context daraltma + Car index'leri
- 2.5 entity `[MaxLength]` + `MinPrice/MaxPrice long` + görsel magic-byte (migration `Faz2DataIntegrity`)
- 2.8 AI prompt-injection çerçevesi + `GeminiModel` config
- 2.4 Serilog + admin audit log (`AuditLog` tablosu, migration `AddAuditLog`)
- 2.R responsive (navbar hamburger, taşma düzeltmeleri, 390/768/1280 doğrulandı)
- 2.2 Tailwind build-time derleme + tüm asset'ler self-host + CSP'den CDN temizlendi
- 2.3 PWA: service-worker yeniden yazıldı (statik-only, network-first HTML, `offline.html`,
  `activate` temizliği, `skipWaiting`/`clients.claim`) + gerçek PNG ikonlar + manifest düzeltme
- 2.7 YouTube import arka plan kuyruğuna alındı (`Channel` + `BackgroundService` + polling durum sayfası)
- 2.6 `OtoRehber.Tests` (xUnit + `WebApplicationFactory`): 23 smoke + mapping + AI birim testi; CI'da zorunlu
- Bağımlılık güvenliği: **AutoMapper kaldırıldı** (elle `CarMappings`), AngleSharp 1.4→1.5,
  System.Text.Json 8.0.0→8.0.5, Caching.Memory 8.0.0→8.0.1, EF Core 8.0.0→8.0.10.
  (SQLitePCLRaw CVE'si: yamalı sürüm yok + SQLite dev-only → `Directory.Build.props`'ta bastırıldı)
- Bonus: admin Create/Edit + Account form'larında jQuery yüklenmiyordu → `_ValidationScriptsPartial`'a eklendi (client-side validation artık çalışıyor)

- DataProtection anahtarları PostgreSQL'e alındı (`Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` +
  `IDataProtectionKeyContext` + migration `AddDataProtectionKeys` [orphan tabloyu güvenle DROP edip yeniden kurar]).
  Restart sonrası oturum korunuyor — yerelde doğrulandı. Bonus: `CustomWebApplicationFactory` gerçek test izolasyonuna
  kavuştu (`ConfigureTestServices` ile DbContext → temp SQLite; önceden bin'deki stale db kullanılıyordu).

### Faz 2 — YAPILMAYAN (sıradaki oturum)
- 2.2 kalanı — inline script'ler için CSP nonce, `<script defer>`, `<environment>` dev/prod ayrımı

**Faz 2 tamamlandı (2.1–2.8, responsive). Faz 3'e geçildi (3.3 SEO yapıldı).**

### Bir sonraki oturumda İLK yapılacak: prod doğrulama
Bu commit Railway'e deploy oldu mu kontrol et:
- `https://otorehber-production.up.railway.app/health` → `Healthy`
- `/`, `/Stats`, `/Compare`, `/Account/Login`, `/AdminCar` → 200
- DevTools → Network: `cdn.`/`unpkg`/`jsdelivr` isteği **olmamalı**, `/lib/...` 200, FA ikonları görünür
- CSP ihlali konsol hatası yok
Sorun varsa: en olası şüpheli `wwwroot/lib/` dosyalarının `.gitignore` / `.dockerignore` ile
elenmesi veya `asp-append-version` + static file cache. `.dockerignore`'da `wwwroot` hariç tutulmadığından emin ol.

---

## 6. Deploy Rehberi (Railway — Adım Adım)

### A) Yerel veritabanı
- `OtoRehber/OtoRehberDB.db*` silindi çünkü `CarReview.UserId` eklendi ve yerel
  Sqlite `EnsureCreated` ile çalışıyor (mevcut şemayı değiştiremez).
- **Yapılacak:** hiçbir şey — `dotnet run` DB'yi yeniden oluşturur, `HasData`
  ile 2 araç (Golf, Corolla) geri gelir.
- **Bundan sonra:** entity değişince yerelde `OtoRehber/OtoRehberDB.db*` sil,
  tekrar çalıştır.
- **Yerelde admin:**
  `cd OtoRehber && dotnet user-secrets set "AdminSeed:Password" "Guclu1!Sifre"`
  → `dotnet run` → `admin@otorehber.com` + o şifre ile giriş.

### B) Hukuki sayfa placeholder'ları
Sadece 2 dosyada var:
- `OtoRehber/Views/Home/Kvkk.cshtml`: `[Şirket/İşletme Unvanı]` (şirket yoksa ad
  soyad), `[adres]`, `[iletişim e-postası]` (2 yer) + satır 14'teki "Not:" cümlesi.
- `OtoRehber/Views/Home/Iletisim.cshtml`: `mailto:[iletişim e-postası]` + görünen
  metin + alttaki "Not:" cümlesi.

### C) Deploy env değişkenleri (Railway → uygulama servisi → Variables → Raw Editor)

> **ÖNCE** projeye PostgreSQL servisi ekle (`+ New → Database → Add PostgreSQL`),
> sonra bu değişkenleri gir. Yoksa `${{Postgres.DATABASE_URL}}` boş kalır ve
> uygulama "geçerli bir PostgreSQL bağlantısı yok" hatası verir.

```
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=Postgres
DATABASE_URL=${{Postgres.DATABASE_URL}}
AdminSeed__Email=<kendi e-postan>
AdminSeed__Password=<güçlü şifre, min 8, büyük/küçük harf + rakam>
GeminiApiKey=<AIza... — aistudio.google.com/apikey>
Resend__ApiKey=<re_... — resend.com/api-keys ; boşsa e-posta gitmez, link log'a yazılır>
Resend__FromEmail=OtoRehber <onboarding@resend.dev>
AllowedHosts=*
```
- `__` = appsettings'teki `:`.
- `DATABASE_URL` = `${{Postgres.DATABASE_URL}}` — Railway değişken referansı.
  "Postgres" DB servisinin adıdır; farklı isim verdiysen ona göre değiştir.
  Kolay yol: Variables → "Add Reference" → Postgres → DATABASE_URL seç.
- Kod `postgresql://user:pass@host/db` biçimini Npgsql biçimine çevirir
  (`Program.cs`). `DATABASE_URL` ve `ConnectionStrings__DefaultConnection`
  ikisi de kabul edilir; `DATABASE_URL` önceliklidir.
- **`AllowedHosts=*` bırak.** Domain'e sabitlersen Railway'in healthcheck
  isteği (farklı Host header) reddedilir → deploy fail. Railway proxy zaten
  routing izolasyonu sağlıyor.
- **Volume/kalıcı disk EKLEME.** DataProtection anahtarları PostgreSQL'de
  (`DataProtectionKeys` tablosu) saklanır. Railway volume'u root sahipliğinde
  mount ettiği için root-olmayan uygulama yazamaz → çöker.
- `healthcheckPath=/health` ve restart politikası `railway.json` ile otomatik.

### D) Google Gemini key
aistudio.google.com/apikey → "Create API key" → `AIza...` kopyala.
- Key kısıtsız olmalı (Application restrictions = None).
- Kod varsayılan modeli `gemini-3.5-flash-lite`. Hesabında farklı bir model daha
  yüksek limitliyse Railway'de `GeminiModel=<model-id>` ile değiştir
  (geçerli id'ler: `gemini-3.5-flash-lite`, `gemini-2.5-flash-lite`, `gemini-2.5-flash`, `gemini-3.5-flash`).
  404 alıyorsan model id'si o key için geçersiz demektir.

### E) Resend (e-posta)
1. resend.com → kaydol (GitHub ile).
2. API Keys → "Create API Key" (Sending access) → `re_...`.
3. Domain yoksa: `Resend__FromEmail=...onboarding@resend.dev` — yalnızca kendi
   Resend hesabı e-postana gönderim yapar (sadece kendi kaydını test edersin).
4. Domain varsa: Resend → Domains → Add Domain → DNS kayıtlarını (SPF/DKIM/DMARC)
   ekle → Verify → `Resend__FromEmail=OtoRehber <no-reply@alanadin.com>`.
5. Key hiç verilmezse: e-postalar Railway loglarına yazılır (manuel test).

### F) Railway deploy adımları
1. `git push` (main güncel olmalı — Railway `main`'e bağlı).
2. railway.com → New Project → Deploy from GitHub repo → `Faruk-Aydn/OtoRehber`.
3. Railway kök `Dockerfile`'ı otomatik bulur.
4. **+ New → Database → Add PostgreSQL**.
5. Uygulama servisi → **Variables** → Raw Editor → yukarıdaki C bloğu
   (+ Add Reference ile `DATABASE_URL`).
6. Volume EKLEME (yukarıdaki nota bak).
7. Deploy yeşil olunca → **Settings → Networking → Generate Domain**.
   `AllowedHosts=*` kalsın.
8. Healthcheck `railway.json` ile otomatik (`/health`).
9. Deployments → View Logs: `Applying migration ..._AddDataProtectionKeys`,
   `Now listening on: http://+:8080`, `Application started`.
10. `https://<domain>/health` → `Healthy`.

### G) Deploy sonrası kontrol
- `/health` Healthy
- admin girişi → Admin Panel
- kayıt → doğrulama e-postası (veya log linki) → doğrula → giriş
- yorum ekle, AI Sihirbaz, karşılaştırma çalışıyor
