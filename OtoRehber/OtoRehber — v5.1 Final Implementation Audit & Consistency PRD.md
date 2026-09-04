# OtoRehber — v5.1 Final Implementation Audit & Consistency PRD

> **Amaç:** v5 PRD'deki Session 0–5 çalışmalarının gerçekten sistemin tamamına doğru şekilde uygulandığını doğrulamak ve kalan tutarsızlıkları düzeltmek.
>
> **KRİTİK:** Bu doküman mevcut sistemi yeniden yazmak için değildir. Mevcut veri, route, entity, component, service ve özellikler korunacaktır. Öncelik mevcut implementasyonların doğru şekilde birbirine bağlanması ve doğrulanmasıdır.

---

# 0. DEĞİŞMEZ KURALLAR

1. Mevcut route'ları koru.
2. 190+ araç verisini silme.
3. Mevcut araç teknik verilerini, motor/şanzıman bilgilerini, kronik sorunları, bakım verilerini, kullanıcı/forum özetlerini, piyasa verilerini ve mevcut AI özelliklerini koru.
4. Aynı işi yapan yeni service/component oluşturma. Mevcut implementasyon varsa onu düzelt.
5. Gereksiz DB migration oluşturma.
6. Frontend'de olmayan backend verisi gerçekmiş gibi gösterilemez.
7. AI ürün kararlarının sahibi değildir.
8. Score, ranking, filtering, diversity, market calculation ve recommendation davranışlarında PRD'de tanımlanmamış yeni matematiksel/ürün kararı alınamaz.
9. Belirsizlik varsa:
   `BLOCKER / DECISION REQUIRED`
10. Her oturum:
   `ANALYZE → PLAN → IMPLEMENT → TEST → REPORT`

---

# 1. BU OTURUMUN ANA AMACI

v5 Session 0–5'in tamamlandığı kabul edilerek aşağıdaki soruların kod seviyesinde kesin olarak cevaplanması:

### 1.1 Canonical Score gerçekten tek kaynak mı?

Aynı Vehicle için:

- Home
- Tüm Araçlar
- Vehicle Detail
- Comparison
- Statistics
- AI Wizard
- Search / Ranking

aynı canonical score'u kullanıyor mu?

### 1.2 Canonical Ranking gerçekten tek kaynak mı?

### 1.3 Presentation Ranking gerçekten canonical ranking'den sonra mı uygulanıyor?

### 1.4 AI Wizard gerçekten backend adaylarını mı açıklıyor?

### 1.5 AI Comparison winner'ı gerçekten backend mi belirliyor?

### 1.6 Listing Analysis backend → UI zinciri tamam mı?

### 1.7 ScoreVersion gerçekten canonical score ile birlikte taşınıyor mu?

### 1.8 Data Confidence ve last_updated tüm ilgili ekranlarda tutarlı mı?

### 1.9 Para birimi ve bakım maliyetleri tüm ekranlarda tutarlı mı?

### 1.10 Eski/çelişkili ürün dili kaldı mı?

---

# 2. STEP 1 — ANALYZE

Bu aşamada önce kod değişikliği yapılmaz.

## 2.1 Canonical Score Audit

Kod içinde şu aramalar yapılmalıdır:

- `score`
- `overallScore`
- `reliability`
- `chronicRisk`
- `maintenanceCost`
- `resaleValue`
- `userSatisfaction`
- `CalculateOtoRehberScore`
- `OtoRehberScore`

Tüm score hesaplama noktaları bulunmalıdır.

Rapor:

| Ekran/API | Score kaynağı | Formula var mı? | Canonical service kullanıyor mu? |
|---|---|---|---|
| Home | | | |
| Vehicles | | | |
| Detail | | | |
| Compare | | | |
| Statistics | | | |
| AI Wizard | | | |
| Search | | | |

### KRİTİK BULGU KONTROLÜ

Aynı araç için farklı endpointlerde farklı score dönüyorsa:

`CANONICAL SCORE INCONSISTENCY`

olarak raporlanmalıdır.

Örnek:

```text
VehicleId = X

Home       = 6.1
Vehicles   = 7.0
Detail     = 6.1
Statistics = 6.1
```

Bu kabul edilemez.

Agent hangi endpoint'in doğru olduğunu kendi başına seçmez.

Önce score'un canonical kaynağını tespit eder.

---

# 3. Canonical Score — ZORUNLU SON DURUM

Tek bir canonical hesaplama kaynağı bulunmalıdır.

Örnek:

```text
CalculateOtoRehberScore(vehicle)
```

veya Session 0'da belirlenen mevcut eşdeğeri.

Formula:

```text
Reliability       × 0.35
Chronic Risk      × 0.25
Maintenance Cost  × 0.20
Resale Value      × 0.15
User Satisfaction × 0.05
```

Toplam:

```text
1.00
```

### Yasak:

- Home'da ayrı score hesabı
- Detail'de ayrı score hesabı
- Statistics'te ayrı score hesabı
- Frontend'de score calculation
- AI'da score calculation
- Controller içinde formula

---

# 4. Score Precision Audit

Canonical score ham decimal olmalıdır.

Örneğin:

```text
8.4267
```

UI:

```text
8.4/10
```

gösterebilir.

Ancak:

```text
8.4
```

sonraki ranking veya comparison hesabında kullanılmamalıdır.

### Kontrol:

Aynı Vehicle için:

```text
rawScore(Home)
rawScore(Detail)
rawScore(Comparison)
rawScore(Statistics)
```

aynı olmalıdır.

---

# 5. ScoreVersion Audit

Canonical score response/model içerisinde:

```json
{
  "overall": 8.4267,
  "version": "v1"
}
```

mantığı korunmalıdır.

ScoreVersion:

- frontend tarafından değiştirilemez
- AI tarafından değiştirilemez
- ranking tarafından yeniden oluşturulamaz
- score hesaplama kaynağıyla tutarlı olmalıdır

DB snapshot sistemi zorunlu değildir.

---

# 6. Ranking Audit

Ranking pipeline:

```text
Vehicle Dataset
      ↓
Valid Data Check
      ↓
Canonical Score
      ↓
Canonical Ranking
      ↓
Diversity / Re-ranking
      ↓
Presentation Ranking
      ↓
Top N
```

### Kontrol edilmesi gerekenler:

- Canonical Ranking ayrı mı?
- Presentation Ranking ayrı mı?
- Diversity score'u değiştiriyor mu?
- Diversity canonical ranking'i yeniden hesaplıyor mu?
- Aynı araç farklı sayfalarda farklı sıralama algoritmasına mı giriyor?
- Ranking deterministic mi?

---

# 7. Diversity Audit

Config:

```text
maxSameMainModel = [PRODUCT DECISION]
```

Agent kendi sayısını seçemez.

Ancak config gerçekten tek noktadan okunmalıdır.

Kontrol:

```text
Hardcoded 1
Hardcoded 2
Hardcoded 3
```

gibi farklı implementationlar varsa:

`DUPLICATED LOGIC`

olarak raporlanmalıdır.

Hierarchy korunmalıdır:

```text
Brand
 ↓
Main Model
 ↓
Generation
 ↓
Engine / Variant
```

Golf 1.6 TDI, Golf 2.0 TDI ve GTI gibi gerçek farklı varyantlar yanlışlıkla tek kayıt haline getirilmemelidir.

---

# 8. Vehicle Detail Audit

Detail sıralaması:

```text
1. Başlık
2. OtoRehber Değerlendirmesi
3. Hızlı Özet
4. Teknik Bilgiler
5. Kronik Sorunlar
6. Bakım / Maliyet
7. Km Bazlı Bakım
8. Artılar / Eksiler
9. Kullanıcı / Forum
10. Yorumlar
11. Piyasa Analizi
12. Benzer Araçlar
13. AI Assistant
```

Kontrol:

- Score doğru mu?
- Evaluation label score ile doğru mu?
- Confidence gösteriliyor mu?
- Update date doğru mu?
- Maintenance data doğru mu?
- Known Issues gerçek DB kayıtlarından mı geliyor?
- Checklist KnownIssue kayıtlarından mı türetiliyor?

---

# 9. Score Label Audit

Sabit eşikler:

| Score | Label |
|---|---|
| 8.0–10.0 | Genel olarak mantıklı |
| 6.5–7.9 | Dikkatli incelenmeli |
| 5.0–6.4 | Riskli |
| 0.0–4.9 | Genel olarak önerilmiyor |

AI bu etiketi belirleyemez.

Örneğin:

```text
6.1 → Riskli
```

olmalıdır.

---

# 10. Data Confidence Audit

Enum:

```text
UNKNOWN
LOW
MEDIUM
HIGH
```

Kontrol:

- UNKNOWN → HIGH yapılmamalı
- creation_date → last_updated yapılmamalı
- Gerçek update tarihi bilinmiyorsa:
  `Güncelleme tarihi bilinmiyor`

gösterilmeli.

Confidence mümkün olduğunca canonical data modelden gelmeli.

Frontend kendi confidence seviyesini üretemez.

---

# 11. Maintenance / Currency Consistency Audit

Bu alan özellikle kontrol edilecektir.

Detail tarafında maliyetler TL gösterilebilirken başka ekranlarda EUR kalmışsa bu tutarsızlık olarak kabul edilir.

Örneğin:

```text
Detail:
27.550 ₺

Statistics:
650 € / yıl
```

gibi aynı ürün içinde farklı para birimi kullanılması kullanıcı deneyimi açısından düzeltilmelidir.

## Kural

Kullanıcıya sunulan ana para birimi:

```text
TRY / ₺
```

olmalıdır.

Backend mevcut EUR maliyetleri tutuyorsa DB şeması gereksiz yere değiştirilmez.

Frontend/backend presentation layer:

```text
EUR source
    ↓
TRY conversion
    ↓
conversion date
    ↓
UI
```

şeklinde çalışabilir.

Dönüşüm tarihi gösterilmelidir.

### Kritik

Kur bilgisi bilinmiyorsa agent kendi kuru uyduramaz.

`BLOCKER / DECISION REQUIRED`

---

# 12. Maintenance Categories

Ayrı göster:

```text
Rutin yıllık bakım
Kilometre bazlı bakım
Büyük arıza riski
```

Tek toplam rakama dönüştürülmemelidir.

---

# 13. AI Wizard Audit

Mevcut Wizard korunur.

Akış:

```text
User Preferences
      ↓
Backend Rule Engine
      ↓
Filtering
      ↓
Canonical Ranking
      ↓
Top Candidates
      ↓
AI Explanation
```

AI:

- araç seçemez
- aday ekleyemez
- aday çıkaramaz
- score değiştiremez
- winner belirleyemez
- DB'de olmayan araç öneremez

### Kod seviyesinde doğrula:

AI API'ya gönderilen payload içerisinde:

```text
candidate vehicle IDs
rank
score
relevant vehicle data
user preferences
```

hangi alanların gönderildiği raporlanmalıdır.

---

# 14. AI Wizard Candidate Integrity Test

Örnek:

Backend:

```json
{
  "candidates": [
    {"vehicleId": 101, "rank": 1},
    {"vehicleId": 52, "rank": 2},
    {"vehicleId": 87, "rank": 3}
  ]
}
```

AI response:

```text
101
52
87
```

dışında yeni araç öneriyorsa response reddedilmelidir.

AI:

```text
"Ben aslında 140 numaralı aracı öneriyorum."
```

diyememelidir.

---

# 15. AI Comparison Audit

Comparison sonucunda backend winner belirlemelidir.

Örnek:

```json
{
  "vehicleA": {...},
  "vehicleB": {...},
  "winner": "vehicleA"
}
```

AI yalnızca açıklama üretir.

### Test

Backend:

```text
winner = A
```

AI:

```text
winner = B
```

derse:

```text
REJECT
```

AI sonucu kullanıcıya doğrudan aktarılmamalıdır.

---

# 16. AI Hallucination Audit

Structured claim sistemi kontrol edilir:

```json
{
  "summary": "...",
  "claims": [
    {
      "type": "known_issue",
      "referenceId": "issue-123"
    }
  ]
}
```

Her claim:

1. DB'de var mı?
2. Vehicle'a ait mi?
3. Type doğru mu?
4. Active/valid mi?

kontrolünden geçmelidir.

Geçemiyorsa:

```text
REJECT
```

---

# 17. AI Numeric Claim Audit

AI'nın aşağıdaki gibi kendi başına sayı üretmesi yasaktır:

```text
"Turbo değişimi 45.000 TL."
```

Eğer backend'de ilgili maintenance/issue reference yoksa sayı gösterilemez.

Aynı kural:

- fiyat
- bakım maliyeti
- yakıt tüketimi
- score
- yüzde
- kilometre
- arıza maliyeti

için geçerlidir.

---

# 18. Listing Analysis Audit

İlan Analizi route'u mevcut sistemde aktif durumdadır.

İlk sürüm alanları:

```text
Vehicle
Year
Mileage
Price
Tramer / Damage
Paint / Replaced count
Optional Note
```

Mevcut UI bunu desteklemektedir.

Ancak sadece formun bulunması yeterli değildir.

## Backend zinciri doğrulanmalıdır:

```text
Listing Input
 ↓
Vehicle Variant Match
 ↓
Market Data
 ↓
Mileage Evaluation
 ↓
Known Issues
 ↓
Maintenance
 ↓
Checklist
 ↓
Result
```

---

# 19. Listing Analysis Result

Sonuç kategorileri:

```text
Fiyat
Kilometre
Kronik Sorunlar
Bakım
Hasar/Boya
Kontrol Listesi
```

### Fiyat

Yeterli market data varsa gerçek hesaplama.

Yoksa:

```text
Yeterli piyasa verisi bulunamadı.
```

### Kilometre

Gerçek variant/mileage data varsa değerlendirme.

Yoksa:

```text
Yeterli veri bulunamadı.
```

### Hasar/Boya

Yalnızca kullanıcı input'u.

Eksik bilgi:

```text
Bilinmiyor
```

---

# 20. Listing Analysis Color Thresholds

Agent kendi threshold'unu seçemez.

Örneğin:

```text
< %10 = green
%10–20 = yellow
> %20 = red
```

gibi değerler PRD'de tanımlı değilse uygulanmayacaktır.

Bunun yerine:

- nötr UI
- ham veri
- veya BLOCKER

kullanılır.

---

# 21. Homepage Audit

Ana sayfa:

```text
Hero
Search
AI Wizard
Comparison
Listing Analysis
OtoRehber Score
Vehicles
Brands
Segments
AI Assistant
```

akışını korur.

Ancak ürün mesajı:

> "Türkiye'nin kapsamlı ikinci el otomobil rehberi"

ile sınırlı kalmamalıdır.

Ana ürün vaadi:

> **"İkinci el araba alırken daha doğru karar vermene yardımcı olur."**

olmalıdır.

Alt açıklama:

> Kronik sorunları, bakım maliyetlerini, piyasa verilerini ve OtoRehber Skoru'nu tek yerde incele.

Bu değişiklik yapılacaksa yalnızca copy değişikliğidir; backend davranışı değiştirilmez.

---

# 22. Aşırı Kesin Dil Audit

Kod/veri içinde şu ifadeler aranmalıdır:

```text
kesin
kesinlikle
ömürlük
sorunsuz
tamamen çözüldü
garanti
asla arıza yapmaz
```

Bunlar gerçek veri tarafından desteklenmiyorsa kaldırılmalıdır.

Özellikle mevcut içeriklerde:

```text
Bakımlısı ömürlük.
```

gibi ifadeler bulunuyorsa düzeltilmelidir.

Yeni dil:

```text
Bakım geçmişi iyi olan örnekleri daha düşük risk taşıyabilir.
```

gibi ölçülü olmalıdır.

---

# 23. "Kimlere Uygun" Audit

Bu bölüm AI tarafından serbestçe yazılmamalıdır.

Rule Engine:

```text
Chronic Issues
+
Performance
+
Fuel
+
Vehicle Segment
```

verilerinden üretmelidir.

AI yalnızca açıklama yapabilir.

---

# 24. Community Data Audit

Review threshold config'den gelmelidir.

Örneğin:

```text
reviewCount < threshold
```

ise:

```text
Topluluk verisi henüz sınırlı.
```

gösterilir.

Agent threshold'u kendi seçemez.

"En çok yorum alan araçlar" gibi başlıkların gerçek review verisi olmadan sosyal kanıt algısı oluşturmadığı doğrulanmalıdır.

---

# 25. Search / Filter Audit

Mevcut filtreler korunmalıdır:

- Marka
- Model
- Yakıt
- Vites
- Kasa
- Çekiş
- Fiyat
- Güvenilirlik
- HP
- Motor hacmi
- Model yılı

Filtreleme sonrası ranking kullanılıyorsa:

```text
Filtered Dataset
 ↓
Canonical Score
 ↓
Canonical Ranking
 ↓
Presentation Diversity
```

mantığı korunmalıdır.

---

# 26. Statistics Audit

Statistics sayfasındaki:

- toplam araç
- toplam yorum
- garage
- ortalama score
- en yüksek score
- bakım maliyeti
- marka ortalamaları

aynı canonical veri kaynaklarından beslenmelidir.

Özellikle:

```text
Average OtoRehber Score
```

frontend'de farklı score hesaplamamalıdır.

---

# 27. Average Score Audit

Ortalama score hesaplanırken:

- rounded score kullanılmaz
- frontend hesaplama yapmaz
- farklı endpointlerden gelen farklı score kullanılmaz

Canonical raw scores kullanılmalıdır.

---

# 28. Vehicle Identity Audit

Her araç için mümkünse:

```text
Brand
Model
Generation
Engine
Transmission
Fuel
YearRange
Variant
```

kimliği açık şekilde korunmalıdır.

Aynı Main Model'in farklı generation/engine varyantları yanlışlıkla birleştirilmemelidir.

---

# 29. API Consistency Audit

Aynı Vehicle için farklı endpointlerin response'ları karşılaştırılmalıdır.

Minimum:

```text
GET vehicle list
GET vehicle detail
GET comparison
GET statistics
GET wizard candidates
```

Score alanları karşılaştırılır.

Örnek test:

```text
VehicleId = 213

Home score       = ?
Vehicle list     = ?
Detail score     = ?
Comparison score = ?
Statistics score = ?
```

Hepsi aynı raw canonical değeri vermelidir.

---

# 30. Frontend Score Calculation Search

Frontend source içinde:

```text
* 0.35
* 0.25
* 0.20
* 0.15
* 0.05
```

veya eşdeğer score formula parçaları aranmalıdır.

Bulunursa:

```text
DUPLICATED SCORE LOGIC
```

olarak işaretlenmelidir.

---

# 31. Backend Score Calculation Search

Backend dışında başka yerde canonical formula bulunuyorsa:

```text
DUPLICATED LOGIC
```

olarak raporlanmalıdır.

Tek canonical kaynak olmalıdır.

---

# 32. Error / Empty State Audit

Her kritik özellikte veri yokluğu düzgün ele alınmalıdır.

### Score:

```text
Yeterli veri olmadığı için OtoRehber Skoru hesaplanamıyor.
```

### Market:

```text
Yeterli piyasa verisi bulunamadı.
```

### Community:

```text
Topluluk verisi henüz sınırlı.
```

### Update date:

```text
Güncelleme tarihi bilinmiyor.
```

Boş alan:

```text
0
5
Tahmini değer
```

ile doldurulmamalıdır.

---

# 33. Performance Audit

Aynı score'un:

- Home
- List
- Detail
- Statistics

için tekrar tekrar ağır şekilde hesaplanıp hesaplanmadığı kontrol edilir.

Mevcut mimariye göre gerekirse:

- caching
- query optimization
- projection
- batch calculation

önerilebilir.

Ancak yeni mimari gereksiz yere kurulmaz.

---

# 34. Security / Validation Audit

Özellikle:

- Vehicle ID
- Comparison vehicle IDs
- Wizard input
- Listing input
- AI claim referenceId

server-side validate edilmelidir.

Frontend validation tek başına yeterli değildir.

---

# 35. STEP 2 — PLAN

Agent önce şu tabloyu oluşturmalıdır:

| Problem | Kaynak | Etkilenen ekran | Risk | Çözüm | DB değişikliği |
|---|---|---|---|---|---|
| Score inconsistency | | | Critical | | |
| Currency inconsistency | | | High | | |
| AI candidate integrity | | | Critical | | |
| Comparison winner | | | Critical | | |
| ScoreVersion | | | High | | |
| Confidence | | | Medium | | |
| Copy / language | | | Medium | | |

---

# 36. STEP 3 — IMPLEMENT

Öncelik sırası:

## P0 — Critical

1. Canonical score inconsistency
2. Frontend/backend duplicate score calculation
3. AI candidate integrity
4. AI comparison winner integrity
5. Canonical ranking inconsistency

## P1 — High

6. ScoreVersion
7. Currency consistency
8. Data confidence consistency
9. Listing Analysis end-to-end validation
10. Statistics canonical data source

## P2 — Medium

11. Copy/language cleanup
12. Empty states
13. Product messaging
14. UI consistency
15. Performance optimization

---

# 37. STEP 4 — TEST

## Score

- [ ] Home score = Detail score
- [ ] Home score = Comparison score
- [ ] Home score = Statistics score
- [ ] Vehicle list score = Detail score
- [ ] Raw score identical
- [ ] UI rounding does not modify raw score
- [ ] Frontend score calculation yok
- [ ] AI score calculation yok
- [ ] Duplicate formula yok
- [ ] ScoreVersion consistent

## Ranking

- [ ] Canonical Ranking deterministic
- [ ] Presentation Ranking deterministic
- [ ] Diversity score değiştirmiyor
- [ ] Diversity weight değiştirmiyor
- [ ] Generation korunuyor
- [ ] Engine/variant korunuyor
- [ ] maxSameMainModel tek config'ten geliyor

## AI

- [ ] AI backend adayları dışına çıkamıyor
- [ ] AI yeni vehicle ID üretemiyor
- [ ] AI winner değiştiremiyor
- [ ] AI yeni score üretemiyor
- [ ] AI olmayan market price üretemiyor
- [ ] Invalid referenceId reject ediliyor
- [ ] Numeric claim reference'a bağlı

## Listing Analysis

- [ ] Vehicle variant doğru eşleşiyor
- [ ] Market data gerçek
- [ ] Mileage değerlendirmesi gerçek
- [ ] KnownIssue kayıtlarından geliyor
- [ ] Maintenance kayıtlarından geliyor
- [ ] Eksik veri tahmin edilmiyor
- [ ] Renk threshold'u uydurulmuyor

## Data

- [ ] UNKNOWN → HIGH otomatik dönüşmüyor
- [ ] creation_date update_date olarak kullanılmıyor
- [ ] Review threshold config'den geliyor
- [ ] Eksik score 0/5 yapılmıyor

## UI

- [ ] 360px
- [ ] 390px
- [ ] 768px
- [ ] 1440px
- [ ] horizontal overflow yok
- [ ] yeni console error yok
- [ ] mevcut route'lar çalışıyor

---

# 38. ZORUNLU GERÇEK VERİ TESTİ

En az 5 farklı araç seç:

1. yüksek score
2. düşük score
3. farklı generation
4. farklı engine
5. eksik community/market data bulunan araç

Her biri için:

```text
Home
↓
Vehicles
↓
Detail
↓
Comparison
↓
Statistics
↓
AI Wizard
```

verileri karşılaştır.

Sonuçlar raporlanmalıdır.

---

# 39. STEP 5 — FINAL REPORT

Agent raporun sonunda mutlaka:

```text
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

## Blockers / Decisions Required
```

bölümlerini doldurmalıdır.

---

# 40. KRİTİK KURAL — "YAPILDI" DEMEK YETERLİ DEĞİL

Bir PRD maddesi:

> "Implemented"

olarak raporlanmadan önce aşağıdaki üç koşulu sağlamalıdır:

```text
CODE
+
API
+
UI
```

Örneğin:

```text
Canonical Score
```

backend'de varsa ama `/araclar` farklı score gösteriyorsa:

```text
NOT IMPLEMENTED / INCONSISTENT
```

kabul edilir.

Aynı şekilde:

```text
AI Architecture
```

prompt'a yazılmış ama AI response'u backend aday listesinin dışına çıkabiliyorsa:

```text
NOT IMPLEMENTED
```

kabul edilir.

---

# 41. FINAL DEFINITION OF DONE

OtoRehber v5.1 ancak şu durumda tamamlanmış kabul edilir:

```text
DATABASE
   ↓
RULE ENGINE
   ↓
CANONICAL SCORE
   ↓
CANONICAL RANKING
   ↓
DIVERSITY / PRESENTATION
   ↓
AI EXPLANATION
   ↓
USER
```

zincirinin gerçek kod akışında doğrulanması.

Ve:

### Score

```text
ONE SOURCE
ONE FORMULA
ONE RAW RESULT
ONE VERSION
```

### Ranking

```text
CANONICAL RANKING
        ↓
PRESENTATION RANKING
```

### AI

```text
AI DOES NOT DECIDE
AI EXPLAINS
```

### Data

```text
UNKNOWN ≠ LOW ≠ MEDIUM ≠ HIGH
```

### Missing Data

```text
NO GUESS
NO FAKE 0
NO FAKE 5
NO AI FILL
```

### Listing Analysis

```text
REAL INPUT
+
REAL VEHICLE DATA
+
REAL MARKET DATA
+
REAL ISSUES
+
REAL MAINTENANCE
```

### User Trust

```text
NO "KESİN"
NO "ÖMÜRLÜK"
NO "KESİNLİKLE AL"
NO INVENTED DATA
```

---

# 42. SON ÜRÜN HEDEFİ

OtoRehber'in amacı:

> **190 araç listeleyen bir web sitesi olmak değil.**

Hedef:

> **Gerçek araç verilerini deterministik kurallar ve kontrollü skor sistemiyle işleyip, kullanıcıya ikinci el araç satın alma kararında yardımcı olan güvenilir bir karar destek platformu olmak.**

Öncelik:

```text
DATA QUALITY
      ↓
SCORE ACCURACY
      ↓
DECISION LOGIC
      ↓
AI RELIABILITY
      ↓
UX CONSISTENCY
      ↓
PERFORMANCE
      ↓
NEW FEATURES
```

**Bu PRD tamamlanmadan yeni büyük özellik eklenmemelidir.**