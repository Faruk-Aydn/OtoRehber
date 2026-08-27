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
| 2026-08-27 | 1.1, 1.4 | Tehlikeli endpoint'ler + AI sahte-veri fallback kaldırıldı | f2b020e |
| 2026-08-27 | 1.2, 1.3 | PostgreSQL desteği + gizli anahtar sertleştirme | e1fde7f |
| 2026-08-27 | 1.5–1.8 | CSRF, e-posta doğrulama, şifre sıfırlama, pipeline sertleştirme | aeba250 |
| 2026-08-27 | 1.9, 1.10 | KVKK/hukuki sayfalar + çerez banner + Docker/CI + README | 12cc109 |
| 2026-08-27 | 1.3, 1.10 | Railway `DATABASE_URL` ayrıştırma + Docker healthcheck düzeltme + deploy rehberi | 93c5dbb |
| 2026-08-27 | deploy | Postgres bağlantı çözümü + `railway.json` | 1e7fb51 |
| 2026-08-27 | deploy | `PORT` env dinleme + healthcheck timeout | 1c974f8 |
| 2026-08-27 | deploy | DataProtection anahtarları PostgreSQL'de (`DataProtectionKeys`); volume kaldırıldı; `.env` deseni | (bu commit) |

**Faz 1 tamamlandı.** Kalan küçük işler: hukuki sayfalardaki `[...]` işletme
bilgileri, prod `AllowedHosts`, gerçek Resend/Gemini/AdminSeed env değerleri.
Sıradaki: Faz 2.

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
