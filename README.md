# OmniConvert.BenchmarkLab

Production kararlarını desteklemek için tasarlanmış, **pipeline-odaklı** TIFF dönüşüm benchmark laboratuvarı.

Bu proje; **raster/image → TIFF**, **PDF → TIFF**, **DOCX → TIFF** ve **XLSX → TIFF** dönüşümlerinde farklı motor ve pipeline yaklaşımlarını karşılaştırır. Amaç yalnızca “en hızlı motoru” bulmak değil, **her dosya türü için en uygun pipeline mimarisini** belirlemektir. Proje .NET 8 tabanlıdır. :contentReference[oaicite:0]{index=0}

---

## Proje Amacı

OmniConvert.BenchmarkLab, farklı dönüşüm motorlarını **tek başına** değil, **pipeline seviyesinde** benchmark etmek için geliştirilmiştir.

Bu yaklaşım sayesinde aşağıdaki soru tiplerine veri temelli cevap verilebilir:

- Hangi dosya türünde hangi motor daha hızlı?
- Hangi pipeline daha düşük RAM kullanıyor?
- Hangi yaklaşım production için daha güvenli?
- Direct TIFF, PDF bridge veya render-merge mimarilerinden hangisi hangi senaryoda avantajlı?

Benchmark çıktıları, daha sonra geliştirilen servis katmanında dosya türüne göre **en uygun pipeline seçimi** yapmak için kullanılmak üzere üretilir.

---

## Mimari

Proje, `Program.cs` içinden tüm benchmark akışını yöneten; senaryo üretimi, pipeline çalıştırma, doğrulama ve CSV raporlama yapan tek bir benchmark host olarak çalışır. Akışta `Benchmarking`, `Core`, `Inputs`, `Pipelines`, `Reporting` ve `Validation` katmanları aktif olarak kullanılır. :contentReference[oaicite:1]{index=1}

| Katman / Alan | Sorumluluk |
|---|---|
| **Core** | Domain modelleri, enum’lar, request/result tipleri, built-in profile tanımları |
| **Inputs** | Raster, PDF, Word ve Excel örnek dosyalarının yüklenmesi |
| **Benchmarking** | Scenario üretimi, benchmark koşusu, özet istatistikler |
| **Pipelines** | Her motor için dönüşüm pipeline implementasyonları |
| **Validation** | TIFF çıktı doğrulama |
| **Reporting** | Konsol özeti ve CSV benchmark raporu üretimi |

---

## Çalışma Modeli

Benchmark host şu ana veri kaynaklarını kullanır:

- `Inputs/raster`
- `Inputs/pdf`
- `Inputs/word`
- `Inputs/excel`

ve çıktılarını tek bir output klasörüne yazar. `Program.cs` içinde raster, PDF, Word ve Excel örnekleri ayrı ayrı yüklenir; bunlardan scenario’lar oluşturulur ve ilgili pipeline listeleriyle benchmark edilir. Ayrıca benchmark sonuçları CSV’ye append edilir. Final benchmark ayarı mevcut kodda **warmupRuns: 1** ve **measuredRuns: 5** olarak görünmektedir. :contentReference[oaicite:2]{index=2}

---

## Benchmark Felsefesi

Bu laboratuvar, motorları yalnızca kütüphane ismi üzerinden değil, **gerçek kullanım biçimi** üzerinden karşılaştırır.

Bu nedenle üç ana pipeline sınıfı vardır:

| Pipeline tipi | Açıklama |
|---|---|
| **DirectNativeTiff** | Motor doğrudan TIFF üretir |
| **BridgeViaPdf** | Belge önce PDF’e dönüştürülür, sonra TIFF üretilir |
| **RenderThenMerge** | Sayfalar image olarak render edilir, sonra multipage TIFF oluşturulur |

`InferPipelineType(...)` içinde bu sınıflandırma açık şekilde kullanılmaktadır. :contentReference[oaicite:3]{index=3}

---

## Benchmark Fazları

Projede şu benchmark fazları aktif olarak yer alır:

| Faz | Açıklama |
|---|---|
| **Raster / Image → TIFF** | JPEG / PNG / TIFF benzeri raster girdiler için TIFF benchmark |
| **PDF → TIFF** | PDF motorlarının benchmark edilmesi |
| **DOCX → TIFF** | Word motorları ve pipeline’larının benchmark edilmesi |
| **XLSX → TIFF** | Excel motorları ve pipeline’larının benchmark edilmesi |

`Program.cs` içindeki pipeline listeleri ve scenario üretimi bu dört fazı açıkça göstermektedir. :contentReference[oaicite:4]{index=4}

---

## Motor Entegrasyonları

### Raster

| Pipeline | Tür | Durum |
|---|---|---|
| `RasterMagickPipeline` | DirectNativeTiff | Full |

### PDF

| Pipeline | Tür | Durum |
|---|---|---|
| `GhostscriptScaledPipeline` | DirectNativeTiff | Full |
| `PdfiumPipeline` | RenderThenMerge | Full |
| `MuPdfPipeline` | RenderThenMerge | Full |
| `AsposePdfPipeline` | DirectNativeTiff | Limited |

PDF benchmark akışında `MuPdfPipeline` için `PdfOcrBinary300` profilinde skip mantığı da bulunmaktadır. :contentReference[oaicite:5]{index=5}

### Word

| Pipeline | Tür | Durum |
|---|---|---|
| `LibreOfficeWordPdfBridgePipeline` | BridgeViaPdf | BridgePipeline |
| `AsposeWordsDirectTiffPipeline` | DirectNativeTiff | EvaluationOnly |
| `SpireWordRenderMergePipeline` | RenderThenMerge | Experimental |
| `SyncfusionWordDirectTiffPipeline` | RenderThenMerge | Experimental |
| `GemBoxWordDirectTiffPipeline` | DirectNativeTiff | EvaluationOnly |

Word benchmark status sınıflandırması doğrudan `Program.cs` içinde tanımlanmıştır. :contentReference[oaicite:6]{index=6}

### Excel

| Pipeline | Tür | Durum |
|---|---|---|
| `LibreOfficeExcelPdfBridgePipeline` | BridgeViaPdf | BridgePipeline |
| `AsposeCellsDirectTiffPipeline` | DirectNativeTiff | EvaluationOnly |
| `SyncfusionExcelRenderMergePipeline` | RenderThenMerge | Full |
| `SpireExcelRenderMergePipeline` | RenderThenMerge | Full |

Excel benchmark status sınıflandırması da `Program.cs` içinde açık şekilde tanımlanmıştır. :contentReference[oaicite:7]{index=7}

---

## Test Profilleri

Projede raster ve office benchmark’ları için built-in profile setleri kullanılır. `Program.cs` içinde şu profile grupları referans alınmaktadır:

- `BuiltInProfiles.RasterMatrixProfiles`
- `BuiltInProfiles.PdfMatrixProfiles`
- `BuiltInProfiles.OfficeAll` :contentReference[oaicite:8]{index=8}

### Office profilleri

| Profil | Amaç | Tipik kullanım |
|---|---|---|
| `OfficeOcrGray300` | OCR odaklı gri tonlu çıktı | Word / Excel OCR senaryoları |
| `OfficeOcrBinary300` | OCR odaklı 1-bit siyah-beyaz çıktı | Word / Excel binary OCR senaryoları |
| `OfficeVisualLzw300` | Görsel sadakat odaklı renkli çıktı | Word / Excel görsel kalite senaryoları |

### PDF profilleri

| Profil | Amaç |
|---|---|
| `PdfOcrGray300` | OCR odaklı gri tonlu PDF rasterizasyonu |
| `PdfOcrBinary300` | Binary OCR odaklı PDF rasterizasyonu |
| `PdfVisualLzw300` | Görsel odaklı renkli PDF rasterizasyonu |
| `PdfVisualJpeg300` | JPEG tabanlı görsel PDF çıktı senaryosu |

### Raster profilleri

Raster benchmark, `BuiltInProfiles.RasterMatrixProfiles` kullanılarak çalıştırılır. :contentReference[oaicite:9]{index=9}

---

## Ölçülen Metrikler

Benchmark sonuçları her scenario ve pipeline için özetlenir ve CSV’ye yazılır. Kod akışına göre aşağıdaki metrik ailesi izlenir:

- başarı / başarısızlık
- elapsed time
- peak private RAM
- output file size
- error count
- output validation sonucu

Ayrıca sonuçlar `BenchmarkStatistics.BuildSummary(...)` ile özetlenip `CsvBenchmarkReporter` üzerinden CSV’ye eklenir. :contentReference[oaicite:10]{index=10}

---

## Scenario Kategorileri

Program akışı, input dosya isimlerinden kategori türetir.

### Genel input kategorileri
- `TextDocument`
- `ScannedDocument`
- `PhotoHeavyDocument`

### Word kategorileri
- `TextWord`
- `TableHeavyWord`
- `ImageShapeWord`
- `UnicodeWord`
- `CorporateTemplateWord`

### Excel kategorileri
- `GeneralExcel`
- `MergedCellsExcel`
- `ChartExcel`
- `PrintAreaExcel`
- `LandscapeExcel`
- `MultiSheetExcel`

Bu sınıflandırmalar `InferInputCategoryFromFileName(...)` ve `InferOfficeInputCategoryFromFileName(...)` içinde tanımlanmıştır. :contentReference[oaicite:11]{index=11}

---

## Final Benchmark Konfigürasyonu

Mevcut benchmark host, raster, PDF, Word ve Excel fazlarını tek çalıştırmada koşturacak şekilde yapılandırılmıştır. `Program.cs` içinde tüm scenario oluşturma çağrılarında final benchmark için:

- `warmupRuns: 1`
- `measuredRuns: 5`

kullanılmaktadır. Aynı dosyada benchmark sonuçları `benchmark_results.csv` dosyasına yazdırılır. :contentReference[oaicite:12]{index=12}

---

## Çalıştırma

Repo bir .NET 8 console benchmark uygulaması olarak yapılandırılmıştır. :contentReference[oaicite:13]{index=13}

Genel akış:

1. Input klasörlerini doldur
2. `Program.cs` içindeki pipeline listelerini ve benchmark status’lerini kontrol et
3. Çalıştır
4. Konsol özetini ve CSV çıktısını incele
5. Gerekirse manuel kalite değerlendirme tablosu ile birlikte nihai karar ver

---

## Benchmark Sonuçlarını Yorumlama Notu

Bu projede bazı pipeline’lar teknik olarak çalışsa bile, doğrudan production kararı için aynı güven seviyesinde değerlendirilmeyebilir.

Örnek statüler:
- **Full** → benchmark’a tam dahil
- **BridgePipeline** → bridge mimarisiyle çalışan ama anlamlı aday
- **EvaluationOnly** → trial / lisans etkisi sonucu etkileyebilir
- **Experimental** → çalışır ama fidelity / scale / kalite ayrıca doğrulanmalıdır
- **Limited** → kısıtlı kullanım veya adil karşılaştırma dışı durum

Bu nedenle karar verirken sadece hız değil:
- kalite,
- sayfa ölçeği,
- layout doğruluğu,
- trial artifact etkisi,
- production suitability

birlikte değerlendirilmelidir.

---

## Mevcut Durum

| Alan | Durum |
|---|---|
| .NET 8 benchmark host | ✅ |
| Raster benchmark | ✅ |
| PDF benchmark | ✅ |
| Word benchmark | ✅ |
| Excel benchmark | ✅ |
| CSV raporlama | ✅ |
| TIFF validation | ✅ |
| Pipeline type classification | ✅ |
| Benchmark status classification | ✅ |
| Multi-phase benchmark orchestration | ✅ |
| Manual quality review workflow | ⬜ (harici değerlendirme süreci) |

---

## Not

Bu repo benchmark laboratuvarıdır. Amaç production service kodunu çalıştırmak değil, **dönüşüm motorları ve pipeline’ları karşılaştırarak doğru production kararını desteklemektir**. Bu nedenle proje, karar kalitesi ve benchmark izlenebilirliği açısından özellikle:
- pipeline ayrımı,
- senaryo kategorileri,
- profile bazlı test,
- CSV raporlama
üzerine kurulmuştur.
