# OtoRehber — Ürün, UX/UI ve Fonksiyonel İyileştirme PRD (v5)

**Vibe Coding / AI Coding Agent Implementation Document**

> Amaç: Mevcut OtoRehber uygulamasını yeniden yazmak değil; mevcut veri modelini, araç detaylarını, kronik sorunları, bakım maliyetlerini, kullanıcı/forum özetlerini, AI Sihirbazı'nı ve AI karşılaştırma sistemini koruyarak ürünü daha profesyonel, güvenilir, anlaşılır ve gerçek kullanıcıya değer veren bir otomobil satın alma yardımcısına dönüştürmek.

**v5 notu:** v4 üzerine 8 son düzeltme uygulandı — Missing Score Component Policy kesinleşti (minimum veri kapsamı sağlanmazsa Overall Score = N/A), Canonical Ranking ile Presentation Ranking kavramsal olarak ayrıldı, `maxSameMainModel` net şekilde "[PRODUCT DECISION REQUIRED]" placeholder olarak işaretlendi, ScoreVersion kavramı eklendi, İlan Analizi sonuç yapısı ve renk-eşiği BLOCKER kuralı netleşti, AI rolü "Clarification Layer" ile genişletildi, Canonical Source Map ve Acceptance Criteria buna göre güncellendi.

---

## 0. DEĞİŞMEZ KURALLAR (Her oturumda geçerli)

1. Mevcut route'ları koru.
2. Mevcut veritabanı yapısını gereksiz yere değiştirme; şema değişikliği gerekiyorsa migration olarak ayrı belgele.
3. Mevcut 190+ araç verisini silme.
4. Mevcut araç detaylarındaki teknik bilgiler, motor/şanzıman, kronik sorunlar, artı/eksi, bakım maliyetleri, km bazlı bakım eşikleri, kullanıcı/forum özetleri, piyasa fiyatları, puanlama, AI özellikleri **korunmalı** — üzerine inşa edilmeli, silinmemeli.
5. Her oturum **STEP 1 (Analiz) → STEP 2 (Plan) → STEP 3 (Uygula) → STEP 4 (Test) → STEP 5 (Rapor)** sırasını takip eder.
6. Aynı işlevi yapan yeni component/service oluşturma; mevcut varsa geliştir.
7. **Mock veri kuralı:** Backend'de karşılığı olmayan veri frontend'de "varmış gibi" gösterilmez. Backend'i olmayan özellik ya aynı oturumda backend'iyle kapsanır, ya da UI'da "Yakında" durumunda devre dışı gösterilir.
8. Her oturum sonunda regresyon kontrolü Ek A'daki somut checklist ile yapılır.

### 0.1 Agent Decision Boundary (KRİTİK — tüm oturumların başına eklenir)

> Agent, ürün davranışını değiştirecek yeni algoritmik kararları **kendisi alamaz**. Özellikle:
> - Score ağırlıkları
> - Score eşikleri
> - AI davranışı
> - Ranking/diversity mantığı
> - Veri confidence kriterleri
> - Filtering kriterleri
> - Recommendation kriterleri
> - Market price calculation
>
> Bu konularda PRD'de tanımlanmamış bir karar gerekiyorsa, agent kendi varsayımıyla production davranışı oluşturmaz — bunu **`BLOCKER / DECISION REQUIRED`** olarak Session Report'ta işaretler ve bir sonraki adıma geçmeden önce insana sorar. Teknik implementasyon detaylarını (değişken adı, dosya organizasyonu, hangi kütüphane) agent kendi seçebilir; ama yukarıdaki listedeki **ürün kararlarını** değiştiremez.
>
> **Skor matematiği özel olarak kapsam dahilindedir:** Agent, eksik veri durumunda score hesaplama yöntemi, normalization, weighting, confidence etkisi veya fallback matematiği hakkında PRD'de açıkça tanımlanmamış bir karar veremez. Özellikle *missing score, missing review, missing market data, missing maintenance data, normalization, weight redistribution, fallback score* konularında kendi matematiksel varsayımını production'a uygulamaz — belirsizlik varsa `BLOCKER / DECISION REQUIRED` raporlanır.

---

## 1. ÜRÜNÜN YENİ KONUMLANDIRMASI

Ana ürün vaadi "araba hakkında bilgi vermek" değil, **"ikinci el araba alırken daha doğru karar vermene yardımcı olmak"**.

| Senaryo | Kullanıcı ihtiyacı | Karşılık gelen özellik |
|---|---|---|
| 1 | "Bu araba nasıl?" | Araç Detay |
| 2 | "Bu iki arabadan hangisini almalıyım?" | AI Karşılaştırma |
| 3 | "Bütçeme/kullanımıma hangi araba uygun?" | AI Sihirbaz |
| 4 | "Bir ilan buldum, mantıklı mı?" | İlan Analizi |
| 5 | "Arabamı aldım, bakımını takip etmek istiyorum." | Garajım |

Mimari prensip (tüm oturumlara hakim):

```
DATABASE → RULE ENGINE → SCORE ENGINE → RANKING → AI EXPLANATION → USER
```

AI: ❌ veri kaynağı değil · ❌ skor motoru değil · ❌ ranking motoru değil · ❌ DB yerine geçmez · ❌ olmayan bilgiyi tamamlamaz
AI: ✅ açıklar · ✅ özetler · ✅ kullanıcı diline çevirir · ✅ backend sonucunun nedenini anlaşılır kılar

---

## SESSION 0 — Veri Mimarisi ve Mevcut Sistem Denetimi (yalnızca analiz, kod değişikliği yok)

**Bu oturumda hiçbir kod değişikliği yapılmaz.** Amaç, Session 1'e sağlam bir zeminle girmek.

### STEP 1 — Analyze

**Veri modeli.** Şu entity'lerin mevcut olup olmadığı, alanları, ilişkileri, nullable/required durumları, enum'ları çıkarılır: `Vehicle, Brand, Model, Generation, Engine, Transmission, KnownIssue, Maintenance, MaintenanceCost, MarketData, Review, Score, User, Garage`, AI recommendation/comparison kayıtları.

**Vehicle hiyerarşisi.** `Brand → Model → Generation → Engine → Transmission/Fuel/Year Range` ayrımının sistemde gerçekten nasıl modellendiği tespit edilir. Aynı otomobilin farklı motor/yıl/trim kombinasyonlarının neden ayrı `Vehicle` kaydı olduğu (veya olmadığı) netleştirilir.

**Skor sistemi.** Skor şu anda nerede hesaplanıyor (frontend/backend/AI), DB'de saklanıyor mu yoksa her istekte yeniden mi hesaplanıyor, kaç farklı yerde (Home/Detail/Comparison/Statistics) ayrı ayrı hesaplama var mı — tespit edilir.

**AI veri akışı.** `User → Frontend → Backend → Vehicle Data → AI → AI Response → Frontend` akışı çıkarılır; AI'ya şu anda hangi verilerin gönderildiği tam olarak listelenir.

**Duplicate veri analizi.** "En güvenilir araçlar" listesindeki Corolla tekrarının kaynağı — raw ranking mi, gruplama eksikliği mi — kod seviyesinde bulunur.

### STEP 2 — Plan (rapor formatı)

| Alan | Mevcut yapı | Sorun | Önerilen yapı | Değişiklik gerekli mi? |
|---|---|---|---|---|
| Vehicle | | | | Evet/Hayır |
| Score | | | | |
| AI | | | | |
| Reviews | | | | |
| Market Data | | | | |

### STEP 3 — Implement
Yok. Bu oturumda yalnızca analiz ve planlama yapılır.

### STEP 4 — Doğrulama
- [ ] Entity ilişkileri doğrulandı mı?
- [ ] Score kaynağı bulundu mu?
- [ ] AI veri akışı bulundu mu?
- [ ] Duplicate root cause bulundu mu?
- [ ] Kritik API endpoint'leri belirlendi mi?

### STEP 5 — Report
1. Mevcut mimari 2. Veri modeli 3. Skor sistemi 4. AI sistemi 5. Duplicate veri problemi 6. Tespit edilen riskler 7. Önerilen değişiklikler 8. DB değişikliği gerekip gerekmediği 9. Yeni endpoint gerekip gerekmediği 10. Session 1 için önerilen başlangıç noktası

**11. Canonical Source Map** — aynı işlevin birden fazla yerde kopyalanıp kopyalanmadığını tespit etmek için:

```
Canonical Score:               → [service/function/file]
Canonical Ranking:              → [service/function/file]
Presentation Ranking:           → [service/function/file]
Diversity / Re-ranking:         → [service/function/file]
AI Wizard candidate selection:  → [service/function/file]
AI Comparison winner:           → [service/function/file]
Market price:                   → [service/function/file]
Known Issues:                   → [entity/service/API]
Maintenance:                    → [entity/service/API]
```

Aynı işlev birden fazla yerde implement edilmişse **`DUPLICATED LOGIC`** olarak raporlanır.

---

## SESSION 1 — Güven, Skor ve Veri Netliği Temeli

**Ön koşul:** Session 0 raporu tamamlanmış ve okunmuş olmalı.

### 1.1 "Uzman Puanı" → "OtoRehber Skoru"
Gerçek insan uzman puanlaması yoksa "Uzman Puanı" ifadesi her yerde kaldırılır, yerine **"OtoRehber Skoru"** + sabit açıklama: *"Teknik özellikler, kronik sorunlar, bakım maliyetleri, kullanıcı deneyimleri ve piyasa verileri dikkate alınarak hesaplanır."*
**Kabul kriteri:** "Uzman Puan*" string'i için grep → 0 sonuç.

### 1.2 Canonical OtoRehber Score — **değiştirilemez ağırlıklar**

> Bu ağırlıklar ürün kararıdır. Agent tarafından değiştirilemez, "iyileştirilemez", yuvarlanamaz.

```
Reliability        × 0.35
Chronic Risk       × 0.25
Maintenance Cost   × 0.20
Resale Value       × 0.15
User Satisfaction  × 0.05
─────────────────────────
Overall Score  (toplam ağırlık = 1.00)
```

Hesaplama **tek bir canonical serviste** yapılır (örn. `CalculateOtoRehberScore(vehicle)`), Session 0'da tespit edilen mimariye uygun konumda. Şu üç kural zorunlu:

- **Frontend skor hesaplamaz.**
- **AI skor hesaplamaz/üretemez.**
- **Controller skor formülü içermez** — yalnızca canonical servisi çağırır.

Home, Vehicle Detail, Comparison, AI Wizard, Statistics, Search/Ranking — hepsi aynı fonksiyonun sonucunu kullanır.

**Kabul kriteri:** Aynı araç için Home / Detail / Comparison / Statistics ekranlarında gösterilen `overall score` birebir aynı olmalı.

#### 1.2.1 Canonical Overall Score Formula ve yuvarlama kuralı

```
Overall Score =
  (Reliability × 0.35) +
  (Chronic Risk × 0.25) +
  (Maintenance Cost × 0.20) +
  (Resale Value × 0.15) +
  (User Satisfaction × 0.05)
```

- Tüm alt skorlar 0.0–10.0 aralığındadır.
- Canonical hesaplama **ham decimal değerler** üzerinden yapılır (örn. `8.4267`).
- **Yuvarlama yalnızca UI gösteriminde** yapılır (`8.4267` → ekranda `8.4 / 10`).
- Backend'deki canonical değer hiçbir noktada yuvarlanmış haliyle saklanıp sonraki hesaplamalarda (ranking, karşılaştırma, ortalama vb.) tekrar kullanılmaz — her zaman ham değer üzerinden işlem yapılır.
- Aynı araç için tüm endpoint ve sayfalar aynı canonical (ham) değeri kullanır; frontend farklı sayfalarda farklı yuvarlama veya yeniden hesaplama yapamaz.

#### 1.2.2 Score Version

Canonical score hesaplamasının bir **version** bilgisi bulunur, örn. `ScoreVersion = "v1"`:
```json
{"overall": 8.4267, "version": "v1"}
```
Score version: algoritmanın hangi versiyonla hesaplandığını belirtir, ileride algoritma değiştiğinde eski/yeni hesaplamaların ayırt edilmesini sağlar. Ranking veya UI tarafından yeniden hesaplanmaz, frontend tarafından değiştirilemez, AI tarafından oluşturulamaz/değiştirilemez.

İlk implementasyonda yalnızca version bilgisinin desteklenmesi yeterlidir — yeni bir DB score-snapshot sistemi kurmak bu aşamada zorunlu değildir. Session 0, `ScoreVersion`'ın mevcut mimariye en uygun şekilde nerede tutulacağını raporlar; gereksiz DB değişikliği yapılmaz.

### 1.3 Alt skorların deterministik tanımı (0.0–10.0 aralığında, hepsi)

- **Reliability:** motor/şanzıman güvenilirliği, kronik arıza sıklığı, büyük mekanik arıza riski. AI bu skoru belirlemez.
- **Chronic Risk:** risk yükseldikçe skor **düşer** (Low→9–10, Medium→6–8, High→3–5, Critical→0–2). Frontend bu yönü asla tersine çevirmez.
- **Maintenance Cost:** maliyet düşükse skor yüksek (Very Low→9–10 … Very High→0–4). Gerçek ₺ eşikleri Session 0'da çıkarılan mevcut veri seti analiz edilerek backend'de belirlenir — agent kendi kafasına göre eşik uydurmaz; eşik netleşmiyorsa `BLOCKER / DECISION REQUIRED` olarak raporlanır.
- **Resale Value:** ikinci el talebi, piyasada bulunma sıklığı, fiyat istikrarı. Veri yetersizse skor uydurulmaz — Data Confidence sistemine düşer (bkz. 1.5).
- **User Satisfaction — yetersiz veri durumu:** Yeterli gerçek kullanıcı verisi yoksa `User Satisfaction = N/A` kabul edilir. Bu durumda:
  - Skor uydurulmaz, AI tarafından tahmin edilmez.
  - Eksik veri `0` olarak kabul **edilmez**.
  - Eksik veri `5` gibi nötr bir puanla doldurulmaz.
  - Kullanıcıya "yeterli veri yok" bilgisi gösterilir.
  - Overall Score hesaplanırken yalnızca mevcut bileşenler kullanılabilir.
  - **Eksik bileşenin ağırlığının yeniden normalize edilip edilmeyeceği bir ürün kararıdır — agent bunu kendisi belirleyemez.** Mevcut veri seti ve mimari bu durumda kesin bir hesaplama gerektiriyorsa, agent kendi matematiksel yöntemini seçmek yerine `BLOCKER / DECISION REQUIRED` oluşturur ve ürün sahibinin kararını bekler.

#### 1.3.1 Missing Score Component Policy (genel kural — tüm bileşenler için geçerli)

Herhangi bir score bileşeninin değeri mevcut değilse:
- Eksik bileşen `0` olarak kabul edilmez.
- Eksik bileşen `5` gibi nötr bir değerle doldurulmaz.
- AI tarafından tahmin edilmez.
- Agent kendi fallback veya normalization matematiğini oluşturamaz.

**Overall Score'un hesaplanabilmesi için gerekli minimum veri kapsamı ürün sahibi tarafından açıkça tanımlanmalıdır** (örn. "en az 4/5 bileşen mevcut olmalı" gibi bir eşik — bu eşik PRD'de netleşmemişse agent bunu kendisi seçmez, `BLOCKER / DECISION REQUIRED` raporlar).

- Minimum veri kapsamı **sağlanmıyorsa**: `Overall Score = N/A`, UI'da sabit mesaj: *"Yeterli veri olmadığı için OtoRehber Skoru hesaplanamıyor."*
- Minimum veri kapsamı **sağlanıyorsa**: eksik bileşenlerin ağırlıkları, PRD'de açıkça tanımlanmış canonical matematik üzerinden ele alınır. Bu matematik PRD'de tanımlı değilse agent `BLOCKER / DECISION REQUIRED` oluşturur, kendi normalization/fallback yöntemini production'a uygulamaz.

**Neden:** Veri eksikliği durumunda farklı araçların farklı matematiklerle puanlanmasını engellemek ve score'un güvenilirliğini korumak için.

### 1.4 Score snapshot vs. dinamik hesaplama
Session 0'da tespit edilen mevcut davranışa göre karar verilir: skor her istekte mi hesaplanıyor, yoksa DB'de mi tutuluyor? DB'de tutuluyorsa: hangi veri değişince yeniden hesaplanıyor, cache var mı, manuel refresh gerekiyor mu — bunlar netleşmeden yeni bir DB yapısı **oluşturulmaz**.

### 1.5 Veri güvenilirliği (Data Confidence) — net kriterli enum

```
UNKNOWN | LOW | MEDIUM | HIGH
```

Agent bu değerleri rastgele atayamaz — kriterler:

| Seviye | Kriter |
|---|---|
| HIGH | Birden fazla güvenilir kaynak + teknik veri doğrulanmış + güncel piyasa verisi + yeterli kullanıcı verisi |
| MEDIUM | Ana teknik veriler mevcut, kaynak sayısı sınırlı, bazı piyasa/kullanıcı verisi eksik |
| LOW | Veri büyük ölçüde tahmini, kaynak sayısı çok az, kullanıcı verisi yok/çok az |
| UNKNOWN | Güven seviyesi belirlenemiyor |

**Default olarak her araca "Tahmini/LOW" atanmaz** — yalnızca gerçekten tahmini veri varsa bu etiket kullanılır. "UNKNOWN" veri asla otomatik "HIGH" gösterilmez.

**Geleceğe uyumluluk notu:** Session 1 kapsamında tek bir araç-seviyesi confidence değeri yeterlidir; alan bazlı confidence (`TechnicalDataConfidence`, `ChronicIssueConfidence`, `MaintenanceConfidence`, `MarketDataConfidence`, `CommunityConfidence` gibi) implement etmek bu oturumda **zorunlu değildir**. Ancak mevcut mimari, ileride tek değerden alan-bazlı sisteme geçişi imkansız/çok maliyetli kılacak şekilde tasarlanmaz (örn. confidence bilgisi tek bir düz string kolonu yerine, ileride genişleyebilecek bir yapıda tutulmalı).

### 1.6 `last_updated` kuralı
`last_updated = creation_date` ataması **kaldırılır**. Gerçek güncelleme tarihi bilinmiyorsa `last_updated = null`, frontend: *"Güncelleme tarihi bilinmiyor"* gösterir. Sistem sahte güncellik hissi vermez.

**Kabul kriteri:** "kesin", "ömürlük", "tamamen çözül*" gibi ifadeler için grep → 0 sonuç (bkz. eski v2 madde 1.3, hâlâ geçerli).

### 1.7 Community data eşiği (configurable)
`reviewCount < threshold` (örn. 10, config'den değiştirilebilir) ise "En çok yorum alan araçlar" gibi güçlü sosyal kanıt başlıkları **gösterilmez**; yerine "Topluluk verisi henüz sınırlı." gösterilir.

---

## SESSION 2 — Araç Detay Sayfası Yeniden Kurgusu

**Ön koşul:** Session 1 tamamlanmış (canonical score ve confidence sistemi burada kullanılıyor).

### 2.1 İçerik sıralaması
1. Başlık → 2. OtoRehber Değerlendirmesi → 3. Hızlı Özet → 4. Teknik bilgiler → 5. Kronik sorunlar → 6. Bakım/maliyet → 7. Km bazlı bakım → 8. Artı/eksi → 9. Kullanıcı/forum özeti → 10. Kullanıcı yorumları → 11. Piyasa analizi → 12. Benzer araçlar → 13. AI Asistan

### 2.2 "OtoRehber Değerlendirmesi" (v2'deki "OtoRehber Kararı" yerine — dil yumuşatıldı)

"Kararı" kelimesi kullanıcıda "bu aracı kesin al" algısı oluşturabilir. Bunun yerine **"OtoRehber Değerlendirmesi"** kullanılır, eşikler aynı kalır:

| Skor aralığı | Etiket |
|---|---|
| 8.0–10.0 | Genel olarak mantıklı |
| 6.5–7.9 | Dikkatli incelenmeli |
| 5.0–6.4 | Riskli |
| 0.0–4.9 | Genel olarak önerilmiyor |

**Yasak ifadeler:** "Alınabilir", "Kesin alınır", "Kesinlikle alınmalı" — bunlar kullanılmaz.

Altında sabit uyarı: *"Bu değerlendirme mevcut teknik, maliyet ve piyasa verilerine dayanır. İkinci el araç alımında bağımsız ekspertiz ve servis geçmişi kontrolü önerilir."*

Eşikler sabit koddur, AI her seferinde yeniden karar vermez.

### 2.3 "Kimlere uygun / uygun değil"
Kural bazlı türetilir (AI serbest metin üretmez) — kronik sorun + performans + yakıt tüketimi verisinden.

### 2.4 Bakım maliyetleri — TL odaklı, kategorilere ayrılmış
Euro varsa backend şeması korunur, frontend'de TL'ye çevrilir, dönüşüm tarihi gösterilir. "Rutin yıllık bakım / 100-150k km arası / büyük arıza riski" ayrı gösterilir, tek kalemde birleştirilmez.

### 2.5 Km bazlı bakım — kullanıcı km'sini girebilsin
Basit statik eşik kontrolüdür, AI çağrısı gerektirmez (gereksiz AI tetiklemesinden kaçınılır).

### 2.6 Satın alma kontrol listesi
Kronik sorun tablosundan otomatik türetilir + sabit maddeler (ekspertiz, hasar kaydı, servis geçmişi).

---

## SESSION 3 — Ranking, Duplicate Model ve Veri Çeşitliliği

### 3.1 Diversity algoritması — **configurable, sabit kod değil**

v2'deki "aynı ana model max 1-2" kuralı **hardcode edilmez**. Sebep: "Golf 1.6 TDI / Golf 2.0 TDI / Golf GTI" gerçekten farklı satın alma seçenekleri olabilir; katı bir sabit kural bu ayrımı yok edebilir.

```
Brand → Main Model → Generation → Engine/Variant
```

Backend'de configurable bir parametre kullanılır: `maxSameMainModel`. **Bu değerin kesin sayısı henüz bir ürün kararı değildir** — agent bunu kendi varsayımıyla `1`, `2`, `3` gibi bir sayıya sabitleyip production'a koyamaz:

```
maxSameMainModel = [PRODUCT DECISION REQUIRED]
```

Ürün sahibi kesin bir değer belirlediğinde (örn. "2"), bu değer PRD'ye şu şekilde yazılır ve o andan itibaren agent tarafından değiştirilemez bir ürün kararı olur:

```
maxSameMainModel = 2   // Bu değer ürün kararıdır ve agent tarafından değiştirilemez.
```

Değer kesinleşene kadar implementasyon **configurable** kalır (kod içinde ayrı ayrı hardcode edilmez, tek bir config/servis üzerinden okunur) ama agent kendi başına bir sayı seçip bunu nihai davranış haline getirmez.

**Önemli sınır:** Diversity uygulanırken yalnızca "Model" adına bakılarak farklı nesiller veya motorlar birleştirilmez. Örneğin Golf 1.6 TDI, Golf 2.0 TDI ve Golf GTI otomatik olarak tek bir kayıt gibi kabul edilmez — gruplama önceliği her zaman `Brand → Main Model → Generation → Engine/Variant` hiyerarşisine göredir. Diversity algoritması farklı generation'ları veya motorları gereksiz yere birleştirmez; kullanıcının karşılaştırmak isteyebileceği gerçek araç varyantlarını kaybetmez.

### 3.2 Ranking pipeline (düzeltilmiş sıra)

```
Vehicle Dataset → Valid Data Check → Score Calculation → Ranking → Diversity / Re-ranking → Top N
```

**Gerekçe:** Diversity filtresi canonical skorun yerine geçmez. Önce araçlar gerçek skorlarına göre deterministik şekilde sıralanır (canonical ranking); ardından kullanıcıya gösterilecek liste diversity kurallarına göre **yeniden düzenlenir** (re-ranking). Böylece:
- Score değişmez, canonical ranking korunur.
- Aynı modelin listeyi domine etmesi engellenir.
- Farklı generation/engine seçenekleri kaybolmaz.

Diversity algoritması score'u **değiştiremez**, yalnızca gösterim sırasını yeniden düzenler. Raw score doğrudan kullanıcıya gösterilmez — final liste her zaman Diversity/Re-ranking adımından geçmiş olmalı.

#### 3.2.1 Canonical Ranking vs Presentation Ranking

Bu iki kavram sistemde **birbirinin yerine kullanılmaz**, ayrı tutulur:

- **Canonical Ranking:** Araçların yalnızca canonical OtoRehber Score kullanılarak deterministik şekilde sıralanmış hali.
- **Presentation Ranking:** Canonical Ranking üzerine configurable diversity/re-ranking kurallarının uygulanmasından sonra kullanıcıya gösterilen final liste.

```
Canonical Score → Canonical Ranking → Diversity / Re-ranking → Presentation Ranking → Top N
```

Diversity/re-ranking: canonical score'u değiştiremez, canonical ranking'in hesaplama mantığını değiştiremez, araçların score değerlerini değiştiremez — yalnızca kullanıcıya sunulan listenin sırasını/çeşitliliğini etkiler.

### 3.3 Variant görünürlüğü
Kartlarda "BMW 5 Serisi" yerine "F10/F11 520d B47 — 2014–2017" gibi net hiyerarşi gösterilir (Güvenilirlik sıralaması, Karşılaştırma, AI Wizard, Search, SEO için önemli).

**Kabul kriteri:** "En güvenilir araçlar" top 10'da aynı ana model, config'de tanımlı `maxSameMainModel` değerinden fazla geçmez; ranking sonucu deterministik olmalı (aynı girdiyle her çalıştırmada aynı sıra).

---

## SESSION 4 — AI Mimarisi: Karar Motoru Değil, Açıklama Katmanı

Bu oturum v2'nin en kritik güncellemesi — AI'nın rolü kökten sınırlandırılıyor.

### 4.1 Yanlış mimari (kullanılmayacak)
```
User → AI → AI 190 araç arasından istediğini seçiyor → Recommendation
```

### 4.2 Doğru mimari
```
User Preferences → Backend Rule Engine → Filter → Ranking → Top Candidates → AI Explanation → User
```

**AI'nın rolü:** AI, OtoRehber'in canonical kararlarını değiştiremez ve kendi başına ürün kararı veremez.

**AI = Explanation + Interpretation + Clarification Layer.**

AI ✅: backend sonuçlarını açıklar · özetler · kullanıcı diline çevirir · kullanıcının kullanım senaryosuna göre sonuçların anlamını yorumlar · karşılaştırma sonuçlarını yorumlar · backend'in sağladığı adayların neden uygun olduğunu açıklar · eksik kullanıcı bilgisi varsa kullanıcıdan açıklayıcı bilgi isteyebilir.

AI ❌: skor hesaplamaz · ranking yapmaz · filtre uygulamaz · kazananı seçmez · backend aday listesini değiştirmez · DB'de olmayan veri oluşturmaz · eksik veriyi tahmin ederek gerçekmiş gibi sunmaz · canonical ürün kararlarını değiştirmez.

AI ≠ Decision Engine, AI ≠ Score Engine, AI ≠ Ranking Engine, AI ≠ Data Source. (Bu tanım AI'nın "sadece pasif metin açıklayan bir sistem" gibi yanlış anlaşılmasını önlemek için "Clarification" bileşenini de içerir — AI, eksik bilgiyi kullanıcıdan sorup toplayabilir, ama bunu kendi varsayımıyla doldurmaz.)

### 4.3 AI Wizard akışı
1. Kullanıcı kriterleri → 2. Backend filtreleme → 3. Backend ranking → 4. İlk 3 aday → 5. AI açıklaması

Backend'den AI'ya giden örnek payload:
```json
{
  "candidates": [
    {"vehicleId": 101, "rank": 1, "score": 8.7},
    {"vehicleId": 52,  "rank": 2, "score": 8.4},
    {"vehicleId": 87,  "rank": 3, "score": 8.1}
  ]
}
```
AI yalnızca bu adayları açıklar, listeye kendi araç ekleyip çıkaramaz. Ancak adaylar arasındaki farkları kullanıcının önceliklerine göre yorumlayabilir:

> ✅ *"Senin düşük bakım maliyeti önceliğin nedeniyle A daha avantajlı. B ise performans konusunda daha güçlü ancak bakım tarafında daha maliyetli."*
> ❌ *"Ben aslında D modelini daha çok öneriyorum."* — bu şekilde backend listesini değiştiremez.

### 4.4 Elenen araç açıklaması — uydurma değil, kayıtlı sebep
Rule engine, hangi kriterin (budget/fuel/transmission/body/usage/priority) elenmeye sebep olduğunu **kaydeder**:
```
BMW 320d — Elendi:
- Bütçe sınırını aşıyor
- Bakım maliyeti önceliğinizle uyuşmuyor
```
AI yalnızca bu kayıtlı sebepleri doğal dile çevirir, kendi gerekçe uydurmaz.

### 4.5 AI Comparison — kazananı backend belirler
```json
{
  "vehicleA": {"reliability": 8.4, "maintenance": 7.1, "resale": 8.8, "overall": 8.1},
  "vehicleB": {"reliability": 7.8, "maintenance": 8.4, "resale": 7.9, "overall": 7.9},
  "winner": "vehicleA"
}
```
AI bundan sonra: neden daha yüksek puan aldı, hangi konuda avantajlı/dezavantajlı, hangi kullanıcı profili için diğer araç mantıklı olabilir — bunları açıklar. AI: winner'ı değiştiremez, yeni score üretemez, backend score'unu yeniden hesaplayamaz, DB'de olmayan karşılaştırma kriteri ekleyemez.

### 4.6 Halüsinasyon kontrolü — structured output ile
v2'deki "AI çıktısını doğal dilden parse edip doğrulama" yaklaşımı yerine **AI structured output** kullanılır:
```json
{
  "summary": "...",
  "claims": [
    {"type": "known_issue", "referenceId": "issue-123"},
    {"type": "maintenance", "referenceId": "maintenance-45"}
  ]
}
```
Backend, her claim için şu kontrolleri yapar:
1. `referenceId` gerçekten mevcut mu?
2. Bu kayıt ilgili `vehicleId` ile ilişkili mi?
3. Claim türü doğru mu?
4. Kayıt aktif/geçerli mi?

Bu kontrollerden herhangi biri başarısız olursa claim **reddedilir (REJECT)**. AI'nin "BMW'nin turbo arızası 45.000 TL'ye mal olur" gibi bir bilgiyi yalnızca serbest metin olarak üretmesine güvenilmez — her sayısal/faktüel iddia mutlaka bir `referenceId`'ye bağlanmalıdır. Böylece "Database → gerçek veri, AI → açıklama katmanı" ayrımı response seviyesinde garanti altına alınır (sadece prompt talimatıyla değil).

### 4.7 AI prompt context (yapılandırılmış)
```
Vehicle: {Brand, Model, Generation, Year, Engine, Transmission, Fuel, Power}
Scores: {Reliability, ChronicRisk, MaintenanceCost, Resale, UserSatisfaction, Overall}
KnownIssues: [{IssueID, Title, Risk, EstimatedCost}]
Maintenance: [{Mileage, Type, EstimatedCost}]
Market: {PriceRange, MarketConfidence}
UserPreferences: {Budget, Usage, Priorities}
```
Context dışında bilgi gerekiyorsa AI'nın sabit cevabı: *"Bu konuda OtoRehber veritabanında yeterli bilgi bulunmuyor."*

### 4.8 Sayfa bağlamına duyarlı AI asistan
Araç detay sayfasındaysa: "Bu F10 520d hakkında ne öğrenmek istiyorsun?" + hızlı seçenekler (Kronik sorunlar / Yakıt tüketimi / Bakım maliyeti / Değerlendirme / Rakipleri).

---

## SESSION 5 — İlan Analizi (yeni özellik, backend önce)

v2'deki yapı korunuyor: **Aşama A (backend/rule engine) → Aşama B (UI)**, ana sayfa CTA'sı yalnızca ikisi de tamamlandığında eklenir.

- İlk sürümde yalnızca backend'in güvenilir şekilde desteklediği alanlar kullanılır (marka, model, yıl, motor, km, fiyat, hasar, boya). Ancak veri modeli, ileride Generation, Şanzıman, Yakıt, Tramer, Değişen, Servis geçmişi, Ekspertiz sonucu, İlan açıklaması gibi alanları da destekleyebilecek şekilde tasarlanır — bu alanların hepsi ilk sürümde implement edilmek zorunda değildir.
- Herhangi bir bilgi eksikse `Unknown` veya *"Yeterli veri bulunamadı."* gösterilir; eksik bilgi **tahmin edilmez**. Örneğin ilan fiyatı için yeterli piyasa verisi yoksa "Bu araç piyasa değerinin %12 altında." gibi bir sonuç **üretilemez** — ya gerçek hesaplanmış bir yüzde gösterilir ya da hiç gösterilmez.
- Kontrol edilmesi gerekenler kronik sorun tablosundan otomatik türetilir (Session 2.6 ile aynı mantık, tekrar kullanılır).

### 5.1 Listing Analysis Result Structure

İlan analizi mümkün olduğunda şu kategoriler üzerinden sonuç üretir:

1. **Fiyat** — piyasa verisi yeterliyse gerçek hesaplanmış karşılaştırma; yetersizse *"Yeterli piyasa verisi bulunamadı."*
2. **Kilometre** — araç/varyant verisine göre değerlendirme; veri yoksa tahmin yapılmaz
3. **Kronik Sorunlar** — ilgili araç/varyantın mevcut `KnownIssue` kayıtları, özellikle kontrol edilmesi gerekenler
4. **Bakım** — mevcut bakım kayıtları ve km eşikleri, yaklaşan bakım/bilinen maliyetler
5. **Hasar/Boya** — yalnızca kullanıcının girdiği gerçek bilgiler; girilmemiş bilgi tahmin edilmez
6. **Kontrol Listesi** — kronik sorunlardan otomatik türetilen maddeler + ekspertiz + hasar kaydı + servis geçmişi

Sonuç mümkün olduğunda şu formatta sunulabilir: Fiyat / Kilometre / Kronik Sorun Riski / Bakım için 🟢🟡🔴 göstergeleri + Kontrol Edilmesi Gerekenler checklist'i.

**Önemli sınır — renk eşikleri agent'ın kararı değildir:** Bu renklerin hangi koşullarda (hangi % sapma, hangi km aralığı) kullanılacağı PRD'de tanımlanmamışsa, agent kendi threshold değerlerini belirleyemez. Threshold tanımlı değilse üç seçenekten biri uygulanır: (a) nötr UI kullanılır (renksiz, sadece veri gösterilir), (b) yalnızca ham veri gösterilir, (c) `BLOCKER / DECISION REQUIRED` raporlanır. AI tarafından üretilen açıklamalar yalnızca backend'den gelen gerçek sonuçlara dayanır.

---

## SONRAKİ (P2/P3 — bu dokümanın kapsamı dışında)
Garajım'ın kişisel takip sistemine dönüşmesi · SEO route yapısı + structured data (yalnızca gerçek veri için) · mobil sticky navigation · admin veri tamamlanma göstergesi · filtre sonuçlarında akıllı sıralama.

---

## Öncelik Sırası

```
SESSION 0 — Data Architecture Audit (sadece analiz)
      ↓
SESSION 1 — Trust + Canonical Score + Data Confidence
      ↓
SESSION 2 — Vehicle Detail UX
      ↓
SESSION 3 — Ranking + Duplicate Model (configurable diversity)
      ↓
SESSION 4 — AI Architecture + Hallucination Control
      ↓
SESSION 5 — Listing Analysis (backend önce, UI sonra)
```

Session 0 tamamlanmadan Session 1–4'te büyük veri mimarisi değişikliği yapılmaz.

---

## Ek A — Regresyon Kontrol Checklist'i (her kod değiştiren oturum sonunda)

- [ ] Ana sayfa yükleniyor, araç listesi görünüyor
- [ ] Filtreleme çalışıyor
- [ ] Araç detay sayfası tüm bölümleriyle render oluyor
- [ ] Karşılaştırma 2 araçla çalışıyor
- [ ] AI Sihirbaz baştan sona bir öneri üretiyor
- [ ] Login/Register bozulmamış
- [ ] Sayfalama doğru link metniyle çalışıyor
- [ ] 360px, 390px, 768px, 1440px genişliklerde taşma yok
- [ ] Konsolda yeni JS hatası yok
- [ ] Aynı araç için Home/Detail/Comparison/Statistics skorları birebir eşleşiyor

## Ek B — Session Report Şablonu (her oturum sonunda doldurulur)

```
## Implemented
## Not Implemented
## Database Changes
## New API Endpoints
## New Environment Variables

## Product Decisions Made
## Product Decisions Not Made
## Assumptions
## Data Integrity Risks
## AI Safety / Hallucination Risks
## Performance Risks
## Regression Risks
## Blockers / Decisions Required   ← agent kendi başına ürün kararı ALMADIYSA burada işaretler
```

## Ek C — Yeni Acceptance Criteria (v3 ile eklenen)

**Score**
- [ ] Aynı araç için Home/Detail/Comparison/Statistics aynı overall score'u gösteriyor
- [ ] Frontend score hesaplamıyor
- [ ] AI score üretemiyor
- [ ] Ağırlıklar tek canonical kaynaktan geliyor, kod içinde tekrar tanımlanmamış
- [ ] Aynı veri setiyle aynı araç için score calculation deterministik sonuç veriyor
- [ ] Aynı araç farklı endpoint'lerden çağrıldığında canonical (ham) score değişmiyor
- [ ] UI yuvarlaması backend canonical score'u değiştirmiyor/geri yazmıyor
- [ ] Eksik veri durumunda agent tarafından tanımlanmamış bir fallback score uygulanmıyor

**AI**
- [ ] AI, DB'de olmayan vehicle ID kullanamıyor / öneremiyor
- [ ] AI, olmayan chronic issue/maintenance cost oluşturamıyor, claim olarak kabul ettiremiyor
- [ ] AI, comparison/wizard "kazananını" kendisi belirlemiyor, backend winner'ı değiştiremiyor
- [ ] AI Wizard'da AI, backend'in belirlediği adayların dışına çıkamıyor
- [ ] AI yeni numeric score oluşturamıyor
- [ ] AI, backend'in vermediği market price bilgisini gerçekmiş gibi sunamıyor
- [ ] Geçersiz `referenceId` içeren claim'ler backend tarafından reddediliyor

**Data Confidence**
- [ ] "UNKNOWN" veri otomatik "HIGH" gösterilmiyor
- [ ] Gerçek update tarihi yoksa `creation_date` kullanılmıyor, `null` + "bilinmiyor" gösteriliyor
- [ ] Yetersiz review verisinde community ranking bölümü gizleniyor

**Ranking**
- [ ] Aynı modelin varyantları config limitini aşmıyor
- [ ] Farklı engine/generation ayrımı korunuyor (aşırı agresif gruplama yok)
- [ ] Ranking sonucu deterministik (aynı girdi → aynı sıra)

**Canonical vs Presentation Ranking**
- [ ] Canonical Ranking yalnızca canonical score/ranking kurallarından oluşuyor
- [ ] Presentation Ranking diversity/re-ranking sonrasında oluşuyor
- [ ] Diversity, canonical score'u ve ağırlıklarını değiştirmiyor
- [ ] Canonical Ranking deterministik
- [ ] Presentation Ranking deterministik
- [ ] Aynı main model için configurable diversity limiti uygulanıyor
- [ ] Generation/engine ayrımı korunuyor

**Score Version**
- [ ] Canonical score bir `ScoreVersion` bilgisine sahip
- [ ] Frontend `ScoreVersion`'ı değiştiremiyor
- [ ] AI `ScoreVersion`'ı değiştiremiyor
- [ ] `ScoreVersion`, score hesaplama kaynağıyla tutarlı

---

## Temel Prensip

> "190 tane arabayı listeleyen bir site" olmak değil, **"kullanıcının ikinci el araç satın alma kararını gerçek veri, kontrollü skor mantığı ve sınırları net çizilmiş AI açıklamalarıyla destekleyen bir karar destek platformu"** olmak.

Öncelik sırası her zaman: **Data Quality → Score Accuracy → Decision Logic → AI Reliability → UX → New Features.**
