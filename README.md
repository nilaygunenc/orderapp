# OrderApp — Mini Sipariş Uygulaması

> .NET 8 Web API + React ile geliştirilmiş, ürün listeleme ve sipariş yönetimi uygulaması.

---

## İçindekiler

1. [Uygulama Nasıl Çalıştırılır?](#1-uygulama-nasıl-çalıştırılır)
2. [Problemi Hangi Parçalara Ayırdınız?](#2-problemi-hangi-parçalara-ayırdınız)
3. [Database Modelini Neden Bu Şekilde Oluşturdunuz?](#3-database-modelini-neden-bu-şekilde-oluşturdunuz)
4. [Kod Organizasyonunu Neden Bu Şekilde Tercih Ettiniz?](#4-kod-organizasyonunu-neden-bu-şekilde-tercih-ettiniz)
5. [Sipariş ve Stok İşlemlerinde Veri Bütünlüğü](#5-sipariş-ve-stok-i̇şlemlerinde-veri-bütünlüğü)
6. [Cache'i Nerede ve Neden Kullandınız?](#6-cachei-nerede-ve-neden-kullandınız)
7. [Stok Değiştiğinde Cache'i Nasıl Yönettiniz?](#7-stok-değiştiğinde-cachei-nasıl-yönettiniz)
8. [Tamamlanmayan / Sadeleştirilen Noktalar](#8-tamamlanmayan--sadeleştirilen-noktalar)
9. [Kullanılan AI Araçları ve Kod Kontrolü](#9-kullanılan-ai-araçları-ve-kod-kontrolü)
10. [Harcanan Süre](#10-harcanan-süre)

---

## 1. Uygulama Nasıl Çalıştırılır?

### Gereksinimler

| Araç | Minimum Versiyon |
|------|-----------------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0 |
| [Node.js](https://nodejs.org) | 18.0 |
| npm | 9.0 |

---

### Backend

```bash
cd backend/OrderApp.API
dotnet restore
dotnet run
```

- API adresi: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

> **Not:** Uygulama ilk açılışta `orderapp.db` adlı SQLite dosyasını otomatik oluşturur,
> EF Core migration'ı uygular ve 5 örnek ürünü seed olarak ekler.
> `dotnet ef database update` komutuna gerek yoktur.

---

### Frontend

```bash
cd frontend
npm install
npm run dev
```

- Uygulama adresi: `http://localhost:5173`

> Vite'ın proxy yapılandırması sayesinde `/api` istekleri otomatik olarak
> `http://localhost:5000` adresine yönlendirilir. Ayrıca CORS ayarı gerekmez.

---

### Testler

```bash
cd backend
dotnet test
```

Toplam **14 test** çalışır; tüm testler geçmeli.

---

## 2. Problemi Hangi Parçalara Ayırdınız?

Problemi iki ana alan ve birbirinden bağımsız sorumluluk katmanlarına böldüm:

### Backend Katmanları

```
backend/OrderApp.API/
├── Domain/
│   └── Entities/          → Saf veri modelleri (Product, Order, OrderItem)
├── Application/
│   ├── DTOs/              → API giriş/çıkış şemaları, validation kuralları
│   ├── Interfaces/        → Servis sözleşmeleri (IProductService, IOrderService)
│   ├── Services/          → Business logic, caching, transaction yönetimi
│   ├── Exceptions/        → NotFoundException, BusinessException
│   └── Common/            → CacheKeys gibi paylaşılan sabitler
├── Infrastructure/
│   ├── Persistence/       → AppDbContext, Fluent API konfigürasyonu, Seed Data
│   └── Middleware/        → Global exception → ProblemDetails dönüşümü
└── Controllers/           → HTTP isteği → Service çağrısı → HTTP yanıtı
```

### Frontend Katmanları

```
frontend/src/
├── api/          → Axios istemcisi, products.js, orders.js (API çağrıları)
├── pages/        → ProductsPage, NewOrderPage, OrdersPage
└── components/
    ├── shared/   → Navbar, Alert, Spinner (yeniden kullanılabilir)
    └── products/ → ProductCard
```

### Test Katmanı

```
backend/OrderApp.Tests/
├── Helpers/    → InMemory DbContext ve MemoryCache factory'leri
└── Services/   → OrderService ve ProductService business senaryoları
```

---

## 3. Database Modelini Neden Bu Şekilde Oluşturdunuz?

### Tablo yapısı

```
Products ──────────────── OrderItems ─────────── Orders
  Id (PK)                   Id (PK)               Id (PK)
  Sku (UNIQUE)              OrderId (FK)          CustomerName
  Name                      ProductId (FK)        OrderDate
  UnitPrice                 Quantity              TotalAmount
  StockQuantity             UnitPrice ◄── snapshot
```

### Tasarım kararları

**`OrderItem.UnitPrice` — fiyat snapshot'ı**

En kritik tasarım kararı budur. Her sipariş kalemi, ürünün o andaki fiyatını kendi `UnitPrice` alanında saklar. `Product.UnitPrice` ileride değişse bile geçmiş siparişin tutarı hiç etkilenmez. Bu sayede:

- `Order.TotalAmount` her zaman oluşturulma anındaki gerçek tutarı gösterir.
- Fiyat geçmişi takibi için ek bir tablo gerekmez.

**İlişki kuralları**

| İlişki | Davranış | Neden |
|--------|----------|-------|
| `Order → OrderItem` | CASCADE DELETE | Sipariş silinince kalemleri de silinmeli |
| `Product → OrderItem` | RESTRICT | Ürün silinse bile sipariş geçmişi kaybolmamalı |

**`decimal(18,2)` veri tipi**

Para değerlerinde `float` veya `double` kullanmak kayan nokta hatasına yol açar. `decimal(18,2)` ile hesaplamalar tam ve tutarlıdır.

**Check constraint'ler**

`UnitPrice >= 0` ve `StockQuantity >= 0` kısıtları hem Fluent API hem de doğrudan veritabanı seviyesinde tanımlandı. Bu, uygulama katmanı devre dışı kalsa dahi verinin bozulmasını engeller.

**Unique index on `Sku`**

Stok kodu tekrarını uygulama yerine veritabanı seviyesinde garanti eder.

---

## 4. Kod Organizasyonunu Neden Bu Şekilde Tercih Ettiniz?

### Fat Controller Önleme — Separation of Concerns

Controller'lar yalnızca şu üç işi yapar:

1. HTTP isteğini alır
2. İlgili servisi çağırır
3. Uygun HTTP status kodu ile yanıt döner

Tüm iş mantığı (stok kontrolü, transaction yönetimi, fiyat hesaplama, cache) `OrderService` ve `ProductService` içindedir. Controller test edilmeden servis tek başına test edilebilir.

### Interface + DI (Dependency Injection)

`IProductService` ve `IOrderService` interface'leri sayesinde:
- `Program.cs`'de `AddScoped<IProductService, ProductService>()` ile kayıt yapılır.
- Test projesinde gerçek DbContext yerine InMemory veritabanı kullanılabilir.
- Gelecekte servislerin implementasyonu değiştirilirse controller kodu etkilenmez.

### Repository Katmanı Eklenmedi

Bu ölçekte ayrı bir repository katmanı gereksiz soyutlama yaratırdı. Servisler doğrudan `AppDbContext` ile çalışır; EF Core zaten bir repository/unit-of-work pattern'i uygular.

### Global Exception Middleware

Her controller'a `try/catch` yazmak yerine tek bir middleware tüm exception'ları yakalar ve RFC 7807 standardına uygun `ProblemDetails` formatında yanıt üretir. Bu HTTP status code eşleşmelerini merkezi bir yerde tutar:

| Exception | HTTP Status |
|-----------|-------------|
| `NotFoundException` | 404 Not Found |
| `BusinessException` | 422 Unprocessable Entity |
| `ArgumentException` | 400 Bad Request |
| Diğer | 500 Internal Server Error |

---

## 5. Sipariş ve Stok İşlemlerinde Veri Bütünlüğü

### Akış

```
İstek gelir
    │
    ▼
Tüm ürünlerin DB'de var olduğu kontrol edilir
    │  hayır → NotFoundException
    ▼
Tüm kalemlerin stoğu yeterli mi kontrol edilir
    │  hayır → BusinessException (tüm yetersiz ürünler listelenir)
    ▼
BeginTransactionAsync() ◄─────────────────────────────┐
    │                                                  │
    ├─ OrderItem'lar oluşturulur (fiyat snapshot alınır)  │
    ├─ Her ürünün stoğu düşürülür                     │
    ├─ Order kaydedilir                               │
    └─ SaveChangesAsync()                             │
         │  hata → RollbackAsync() ──────────────────┘
         ▼
    CommitAsync()
         │
         ▼
    Cache invalidation (etkilenen ürünler)
```

### Neden transaction açıkça kullanıldı?

`SaveChangesAsync()` tek bir `DbContext.SaveChanges` çağrısını atomik yapar, ancak bu yalnızca EF'in izlediği değişiklikler için geçerlidir. `BeginTransactionAsync()` ile açık transaction kullanılmasının sebebi:

- Stok güncelleme ve sipariş oluşturma mantıksal olarak tek bir iş birimidir.
- İleride ayrı servisler veya raw SQL eklense dahi transaction sınırı net kalır.
- Herhangi bir hata durumunda `RollbackAsync()` ile hiçbir değişiklik DB'ye yazılmaz.

### Tüm stok hataları bir arada döner

Yalnızca ilk yetersiz stoğu bildirmek yerine tüm hatalı ürünler `BusinessException.Errors` listesinde toplanır. Kullanıcı tek istekte hangi ürünlerin sorunlu olduğunu görür.

---

## 6. Cache'i Nerede ve Neden Kullandınız?

`IMemoryCache` ile yalnızca **ürün okuma** endpoint'lerinde cache kullanıldı.

### Cache key yapısı

| Key | İçerik | Süre |
|-----|--------|------|
| `products:all` | Tüm ürün listesi (arama yok) | 10 dakika |
| `products:search:{term}` | Arama sonuçları (terim bazlı) | 10 dakika |
| `products:detail:{id}` | Tek ürün detayı | 10 dakika |

### Neden ürünler cache'lendi?

- Ürün listesi uygulamanın en sık okunan verisidir (her sipariş oluşturmada tekrar listelenir).
- Admin paneli olmadığından ürün fiyatı dışarıdan değiştirilemiyor; stok ise sipariş sonrası invalidate ediliyor.
- 10 dakikalık TTL, olası tutarsızlığı sınırlı bir süreyle tolere eder.

### Neden siparişler cache'lenmedi?

- Her sipariş oluşturulduğunda liste değişir; cache hit oranı düşük olurdu.
- Sipariş detayları güncel gösterilmeli; eski veriyi göstermek kullanıcı deneyimini bozar.

---

## 7. Stok Değiştiğinde Cache'i Nasıl Yönettiniz?

Sipariş başarıyla oluşturulduktan sonra `InvalidateProductCaches` metodu çağrılır:

```csharp
private static void InvalidateProductCaches()
{
    // Liste, detay ve arama cache'lerinin ortak token'ını iptal et
    CacheKeys.InvalidateProducts();
}
```

### Arama cache'leri için strateji

Liste, detay ve arama cache kayıtları ortak bir expiration token kullanır. Sipariş başarıyla
tamamlandığında token iptal edilerek stok bilgisi içeren tüm ürün cache kayıtları anında
geçersiz kılınır. Böylece arama sonuçlarında eski stok gösterilmez.

---

## 8. Tamamlanmayan / Sadeleştirilen Noktalar

| Alan | Yapılan | Atlandı / Sadeleştirildi |
|------|---------|--------------------------|
| **Authentication** | — | Kullanıcı girişi, JWT, rol yönetimi |
| **Cache** | IMemoryCache (in-process) | Redis / distributed cache |
| **Cache invalidation** | Ortak expiration token ile anında temizleme | Dağıtık cache |
| **Pagination** | — | Ürün ve sipariş listelerinde sayfalama |
| **Integration test** | Service katmanı testleri | Controller/HTTP katmanı (`WebApplicationFactory`) |
| **Docker** | — | Dockerfile, docker-compose |
| **Logging** | ASP.NET Core default logger | Serilog / structured logging |
| **Production CORS** | Hardcoded `localhost:5173` | Environment variable ile yönetim |
| **Frontend state** | Component local state | Redux / Zustand / React Query |

---

## 9. Kullanılan AI Araçları ve Kod Kontrolü

### Kullanılan araçlar

- **Kiro (Claude tabanlı AI geliştirme ortamı)** — adım adım prompt'larla kod üretimi ve mimari danışmanlık için kullanıldı.
- **OpenAI Codex** — genel kod incelemesi, performans optimizasyonları ve eksiklerin doğrulanması için kullanıldı.

### Üretilen kodlar nasıl kontrol edildi?

AI tarafından üretilen kod kör olarak kabul edilmedi; her adımda aşağıdaki kontroller yapıldı:

**Entity ve migration tutarlılığı**
- `AppDbContext` Fluent API konfigürasyonu ile migration dosyasındaki tablo tanımları karşılaştırıldı.
- Check constraint'lerin hem C# hem SQL tarafında doğru tanımlandığı doğrulandı.

**Business logic doğruluğu**
- `OrderService.CreateOrderAsync` içindeki stok kontrolü, transaction akışı ve fiyat snapshot mantığı satır satır incelendi.
- "Bir ürün yetersizse hiçbir stok düşmemeli" kuralının `BusinessException` fırlatıldığında transaction rollback ile sağlandığı test edildi.

**Cache davranışı**
- `InvalidateProductCaches` metodunun hangi key'leri sildiği ve hangilerinin TTL'e bırakıldığı gözden geçirildi.
- `CacheKeys` static sınıfındaki key format'larının servis içindeki kullanımla eşleştiği kontrol edildi.

**Test senaryoları**
- Testler beklenen exception tiplerini ve DB durumlarını (`StockQuantity` değerini) doğruluyor mu kontrol edildi.
- `TestDbContextFactory.Create()` her test için izole veritabanı oluşturuyor mu gözlemlendi.

**DTO — Entity eşleşmeleri**
- Response DTO'lardaki alan adları ve tipler, entity alanlarıyla karşılaştırıldı.
- `OrderItemResponseDto.LineTotal` hesaplama (`UnitPrice * Quantity`) doğrulandı.

---

## 10. Harcanan Süre

| Aşama | Süre |
|-------|------|
| Mimari planlama, entity ve veritabanı tasarımı | ~45 dk |
| Backend servisler, controller, middleware | ~1.5 saat |
| Test yazımı ve doğrulama | ~45 dk |
| React frontend | ~1 saat |
| Migration dosyası ve son kontroller | ~30 dk |
| README hazırlığı | ~30 dk |
| **Toplam** | **~5 saat** |

---

## Proje Yapısı

```
merdus/
├── README.md
├── .gitignore
├── backend/
│   ├── OrderApp.sln
│   ├── OrderApp.API/
│   │   ├── Controllers/
│   │   │   ├── ProductsController.cs
│   │   │   └── OrdersController.cs
│   │   ├── Domain/Entities/
│   │   │   ├── Product.cs
│   │   │   ├── Order.cs
│   │   │   └── OrderItem.cs
│   │   ├── Application/
│   │   │   ├── Common/CacheKeys.cs
│   │   │   ├── DTOs/
│   │   │   ├── Exceptions/
│   │   │   ├── Interfaces/
│   │   │   └── Services/
│   │   ├── Infrastructure/
│   │   │   ├── Middleware/
│   │   │   └── Persistence/
│   │   ├── Migrations/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   └── OrderApp.Tests/
│       ├── Helpers/
│       └── Services/
└── frontend/
    ├── src/
    │   ├── api/
    │   ├── components/
    │   ├── pages/
    │   ├── App.jsx
    │   └── main.jsx
    ├── index.html
    └── package.json
```

---

## API Endpoint'leri

| Method | URL | Açıklama |
|--------|-----|----------|
| `GET` | `/api/products` | Tüm ürünleri listele |
| `GET` | `/api/products?search={term}` | İsim veya SKU ile ara |
| `GET` | `/api/products/{id}` | Ürün detayı |
| `POST` | `/api/orders` | Yeni sipariş oluştur |
| `GET` | `/api/orders` | Tüm siparişleri listele |
| `GET` | `/api/orders/{id}` | Sipariş detayı |

### Örnek sipariş isteği

```json
POST /api/orders
{
  "customerName": "Ali Yılmaz",
  "items": [
    { "productId": 1, "quantity": 2 },
    { "productId": 3, "quantity": 1 }
  ]
}
```

### Örnek hata yanıtı (yetersiz stok)

```json
HTTP 422 Unprocessable Entity
{
  "type": null,
  "title": "İş Kuralı İhlali",
  "status": 422,
  "detail": "Bir veya daha fazla iş kuralı ihlali oluştu.",
  "instance": "/api/orders",
  "errors": [
    "Mekanik Klavye (KB-001) için yeterli stok yok. İstenen: 99, Mevcut: 50"
  ]
}
```
#   w e b s i t e  
 #   w e b s i t e  
 #   w e b s i t e  
 