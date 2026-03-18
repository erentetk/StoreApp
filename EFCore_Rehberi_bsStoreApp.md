# EF Core Rehberi - bsStoreApp Projesi Üzerinden

Bu doküman, bulunduğumuz **bsStoreApp** çözümü üzerinden EF Core mantığını anlatmak için hazırlanmıştır.
Anlatım boyunca mevcut dosyalar kullanılmış, proje içinde henüz olmayan ilişki örnekleri için ise **bu projeye nasıl eklenirdi** yaklaşımıyla örnekler verilmiştir.

---

## 1) Bu projede EF Core nerede kullanılıyor?

Bu projede EF Core ağırlıklı olarak aşağıdaki dosyalarda karşımıza çıkıyor:

- `Repositories/EFCore/RepositoryContext.cs`
- `Repositories/EFCore/RepositoryBase.cs`
- `Repositories/EFCore/BookRepository.cs`
- `Repositories/EFCore/Config/BookConfig.cs`
- `WebApi/Extensions/ServicesExtensions.cs`
- `WebApi/ContextFactory/RepositoryContextFactory.cs`
- `WebApi/appsettings.json`

Kısa rol dağılımı:

- **Entity**: `Entities/Models/Book.cs`
- **DbContext**: `Repositories/EFCore/RepositoryContext.cs`
- **Entity yapılandırması / seed data**: `Repositories/EFCore/Config/BookConfig.cs`
- **Repository katmanı**: `Repositories/EFCore/RepositoryBase.cs`, `Repositories/EFCore/BookRepository.cs`
- **Service katmanı**: `Services/BookManager.cs`
- **API katmanı**: `Presentation/Controllers/BooksController.cs`
- **Bağlantı ve DI ayarları**: `WebApi/Extensions/ServicesExtensions.cs`

---

## 2) EF Core nedir?

**Entity Framework Core (EF Core)**, .NET uygulamalarında veritabanı ile nesne tabanlı çalışmayı sağlayan bir **ORM**'dir.

ORM sayesinde:

- Veritabanı tablolarını sınıf gibi düşünebiliriz.
- Satırları nesne gibi kullanabiliriz.
- SQL yazmadan LINQ ile sorgu yazabiliriz.
- Insert / Update / Delete işlemlerini C# koduyla yapabiliriz.

Bu projedeki en temel EF Core sınıfı şudur:

**Dosya:** `Repositories/EFCore/RepositoryContext.cs`

```csharp
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.EFCore.Config;

namespace Repositories.EFCore
{
    public class RepositoryContext : DbContext
    {
        public RepositoryContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new BookConfig());
        }
    }
}
```

Buradaki kavramlar:

- `DbContext`: Veritabanı oturumu gibi çalışır.
- `DbSet<Book>`: `Book` tablosunu temsil eder.
- `OnModelCreating`: entity ilişkileri, kurallar ve seed data burada tanımlanır.

Bu projedeki `Book` entity'si:

**Dosya:** `Entities/Models/Book.cs`

```csharp
namespace Entities.Models
{
    public class Book
    {
        public int Id { get; set; }
        public String Title { get; set; }
        public decimal Price { get; set; }
    }
}
```

Yani EF Core, bu `Book` sınıfını veritabanındaki bir tabloya eşlemeye yarıyor.

---

## 3) Bu proje Code First mi, Database First mü?

Bu proje mantık olarak **Code First** yaklaşımına daha yakındır.

### Code First mantığı

Önce kod yazılır:

- Entity sınıfları oluşturulur.
- `DbContext` yazılır.
- Fluent API / Config sınıfları tanımlanır.
- Sonra migration ile veritabanı oluşturulur veya güncellenir.

Bu projede Code First olduğunu gösteren parçalar:

1. Önce model var:
   - `Entities/Models/Book.cs`

2. Sonra DbContext var:
   - `Repositories/EFCore/RepositoryContext.cs`

3. Sonra config/seed var:
   - `Repositories/EFCore/Config/BookConfig.cs`

```csharp
public class BookConfig : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.HasData(
            new Book { Id = 1, Title = "Karagöz ve Hacivat", Price = 75 },
            new Book { Id = 2, Title = "Mesnevi", Price = 175 },
            new Book { Id = 3, Title = "Devlet", Price = 375 }
        );
    }
}
```

Yani önce sınıf tanımlanıyor, sonra veritabanı bu sınıflara göre şekilleniyor.

### Database First mantığı

Database First yaklaşımında süreç tersidir:

- Önce hazır bir veritabanı vardır.
- Sonra EF Core o veritabanından sınıf üretir.

Yani:

- Code First = **Koddan veritabanına**
- Database First = **Veritabanından koda**

Bu projedeki yapı Database First değil; çünkü burada entity ve context tarafı zaten kodla tanımlanmış durumda.

---

## 4) Migration nedir?

**Migration**, veritabanı şemasındaki değişiklikleri versiyonlu şekilde yönetmemizi sağlar.

Örnek:

- Önce `Book` tablosunda sadece `Id`, `Title`, `Price` vardı.
- Sonra `Stock` alanı ekledik.
- EF Core bu değişikliği migration olarak kaydeder.
- Sonra bu migration veritabanına uygulanır.

### Bu projede migration için önemli dosya

**Dosya:** `WebApi/ContextFactory/RepositoryContextFactory.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Repositories.EFCore;

namespace WebApi.ContextFactory
{
    public class RepositoryContextFactory
        : IDesignTimeDbContextFactory<RepositoryContext>
    {
        public RepositoryContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<RepositoryContext>()
                .UseNpgsql(configuration.GetConnectionString("sqlConnection"),
                prj => prj.MigrationsAssembly("WebApi"));

            return new RepositoryContext(builder.Options);
        }
    }
}
```

Bu sınıf, `dotnet ef` komutları çalıştırılırken EF Core'un `RepositoryContext` nesnesini nasıl oluşturacağını söyler.

### Bu projede migration komutları

Çözüm kökünden örnek komutlar:

```bash
dotnet ef migrations add InitialCreate --project WebApi --startup-project WebApi --context RepositoryContext
dotnet ef database update --project WebApi --startup-project WebApi --context RepositoryContext
dotnet ef migrations remove --project WebApi --startup-project WebApi --context RepositoryContext
```

### Migration ne zaman kullanılır?

- Yeni tablo eklerken
- Alan eklerken / silerken
- İlişki eklerken
- Seed data yapısı değiştiğinde

---

## 5) Veritabanı bağlantısı bu projede nasıl kurulmuş?

**Dosya:** `WebApi/Extensions/ServicesExtensions.cs`

```csharp
public static void ConfigureSqlContext(this IServiceCollection services,
    IConfiguration configuration) => services.AddDbContext<RepositoryContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("sqlConnection")));
```

Burada:

- `RepositoryContext`, Dependency Injection container'a ekleniyor.
- Veritabanı sağlayıcısı olarak **PostgreSQL** kullanılıyor.
- `UseNpgsql(...)` ile bağlantı cümlesi okunuyor.

Bağlantı bilgisi de şurada:

**Dosya:** `WebApi/appsettings.json`

```json
"ConnectionStrings": {
  "sqlConnection": "Host=localhost;Port=5432;Database=bsStoreApp;Username=postgres;Password=erentetik;",
  "sqlConnectionSqlserver": "Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog = bsStoreApp; Integrated Security=true;"
}
```

---

## 6) CRUD işlemleri bu projede nasıl yapılıyor?

CRUD =

- **C**reate
- **R**ead
- **U**pdate
- **D**elete

Bu projede akış şu şekilde:

**Controller -> Service -> Repository -> EF Core(DbContext) -> Database**

Yani `BooksController`, doğrudan `DbContext` ile konuşmuyor.
Önce service'e gidiyor, service repository'yi çağırıyor, repository de EF Core'u kullanıyor.

---

## 7) Repository katmanındaki EF Core kullanımı

### 7.1 Generic repository tabanı

**Dosya:** `Repositories/EFCore/RepositoryBase.cs`

```csharp
public abstract class RepositoryBase<T> : IRepositoryBase<T>
    where T : class
{
    protected readonly RepositoryContext _context;

    public RepositoryBase(RepositoryContext context)
    {
        _context = context;
    }

    public void Create(T entity) => _context.Set<T>().Add(entity);
    public void Delete(T entity) => _context.Set<T>().Remove(entity);

    public IQueryable<T> FindAll(bool trackChanges) =>
        !trackChanges ?
        _context.Set<T>().AsNoTracking() :
        _context.Set<T>();

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression,
        bool trackChanges) =>
        !trackChanges ?
        _context.Set<T>().Where(expression).AsNoTracking() :
        _context.Set<T>().Where(expression);

    public void Update(T entity) => _context.Set<T>().Update(entity);
}
```

### Buradaki önemli mantık

#### `Create`

```csharp
_context.Set<T>().Add(entity);
```

Entity ekler ama veritabanına hemen yazmaz.
Asıl kayıt `SaveChanges()` ile olur.

#### `Delete`

```csharp
_context.Set<T>().Remove(entity);
```

Entity silinmek üzere işaretlenir.

#### `FindAll` ve `FindByCondition`

Burada `trackChanges` parametresi çok önemlidir.

```csharp
!trackChanges ? _context.Set<T>().AsNoTracking() : _context.Set<T>()
```

- `trackChanges = false` ise `AsNoTracking()` kullanılır.
- Bu, sadece okuma işlemlerinde performans avantajı sağlar.
- Güncelleme yapılacaksa genelde `trackChanges = true` tercih edilir.

---

## 8) Book repository içindeki CRUD örnekleri

**Dosya:** `Repositories/EFCore/BookRepository.cs`

```csharp
public class BookRepository : RepositoryBase<Book>, IBookRepository
{
    public BookRepository(RepositoryContext context) : base(context)
    {
    }

    public void CreateOneBook(Book book) => Create(book);
    public void DeleteOneBook(Book book) => Delete(book);

    public IQueryable<Book> GetAllBooks(bool trackChanges) =>
        FindAll(trackChanges)
        .OrderBy(b => b.Id);

    public Book GetOneBookById(int id, bool trackChanges) =>
        FindByCondition(b => b.Id.Equals(id), trackChanges)
        .SingleOrDefault();

    public void UpdateOneBook(Book book) => Update(book);
}
```

Bu sınıfta EF Core sorguları repository desenine uyarlanmış durumda.

---

## 9) Service katmanında CRUD mantığı

**Dosya:** `Services/BookManager.cs`

### 9.1 Create

```csharp
public Book CreateOneBook(Book book)
{
    _manager.Book.CreateOneBook(book);
    _manager.Save();
    return book;
}
```

Akış:

1. Repository'ye ekleme isteği gider.
2. `_manager.Save()` ile `SaveChanges()` çalışır.
3. Veri veritabanına yazılır.

### 9.2 Read - Tüm kitapları getirme

```csharp
public IEnumerable<Book> GetAllBooks(bool trackChanges)
{
    return _manager.Book.GetAllBooks(trackChanges);
}
```

### 9.3 Read - Tek kitap getirme

```csharp
public Book GetOneBookById(int id, bool trackChanges)
{
    var book = _manager.Book.GetOneBookById(id,trackChanges);
    if (book is null)
         throw new BookNotFoundException(id);
    return book;
}
```

Burada servis katmanı iş kuralı ekliyor:

- Kitap yoksa exception fırlatıyor.

### 9.4 Update

```csharp
public void UpdateOneBook(int id, BookDtoForUpdate bookDto, bool trackChanges)
{
    var entity = _manager.Book.GetOneBookById(id, trackChanges);
    if (entity is null)
        throw new BookNotFoundException(id);

    entity = _mapper.Map<Book>(bookDto);

    _manager.Book.Update(entity);
    _manager.Save();
}
```

Bu projede güncelleme DTO + AutoMapper ile yapılıyor.

Kullanılan DTO:

**Dosya:** `Entities/DataTransferObjects/BookDtoForUpdate.cs`

```csharp
public record BookDtoForUpdate(int Id, String Title, decimal Price);
```

AutoMapper profili:

**Dosya:** `WebApi/Utilities/AutoMapper/MappingProfile.cs`

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<BookDtoForUpdate, Book>();
    }
}
```

> Not: EF Core tarafında daha yaygın yaklaşım, var olan tracked entity üzerine map etmektir:
>
> ```csharp
> _mapper.Map(bookDto, entity);
> _manager.Save();
> ```
>
> Ama mevcut projede anlatım amacıyla yeni `Book` nesnesi üretilip `Update(...)` çağrısı yapılmış.

### 9.5 Delete

```csharp
public void DeleteOneBook(int id, bool trackChanges)
{
    var entity = _manager.Book.GetOneBookById(id, trackChanges);
    if (entity is null)
        throw new BookNotFoundException(id);

    _manager.Book.DeleteOneBook(entity);
    _manager.Save();
}
```

Silme işleminde önce kayıt bulunuyor, sonra siliniyor.

---

## 10) Controller katmanında CRUD kullanımı

**Dosya:** `Presentation/Controllers/BooksController.cs`

### 10.1 GET all

```csharp
[HttpGet]
public IActionResult GetAllBooks()
{
    var books = _manager.BookService.GetAllBooks(false);
    return Ok(books);
}
```

### 10.2 GET by id

```csharp
[HttpGet("{id:int}")]
public IActionResult GetOneBook([FromRoute(Name = "id")] int id)
{
    var book = _manager.BookService.GetOneBookById(id, false);
    return Ok(book);
}
```

### 10.3 POST

```csharp
[HttpPost]
public IActionResult CreateOneBook([FromBody] Book book)
{
    if (book is null)
        return BadRequest();

    _manager.BookService.CreateOneBook(book);
    return StatusCode(201, book);
}
```

### 10.4 PUT

```csharp
[HttpPut("{id:int}")]
public IActionResult UpdateOneBook([FromRoute(Name = "id")] int id,
    [FromBody] BookDtoForUpdate bookDto)
{
    if (bookDto is null)
        return BadRequest();

    _manager.BookService.UpdateOneBook(id, bookDto, true);
    return NoContent();
}
```

Burada `trackChanges = true` gönderilmesi mantıklıdır; çünkü güncelleme akışı vardır.

### 10.5 DELETE

```csharp
[HttpDelete("{id:int}")]
public IActionResult DeleteOneBook([FromRoute(Name = "id")] int id)
{
    _manager.BookService.DeleteOneBook(id, false);
    return NoContent();
}
```

### 10.6 PATCH

Projede ekstra olarak `PATCH` de var:

```csharp
[HttpPatch("{id:int}")]
public IActionResult PartiallyUpdateOneBook([FromRoute(Name = "id")] int id,
    [FromBody] JsonPatchDocument<Book> bookPatch)
{
    var entity = _manager.BookService.GetOneBookById(id, true);

    bookPatch.ApplyTo(entity);
    _manager.BookService.UpdateOneBook(id,
        new BookDtoForUpdate(entity.Id,entity.Title,entity.Price),
        true);

    return NoContent();
}
```

---

## 11) One-to-many ilişki nasıl kurulur?

### Mevcut durum

Bu projede şu anda sadece `Book` entity'si var. Yani mevcut kodda henüz bir navigation property veya ilişki tanımlı değil.

Bu yüzden `one-to-many` anlatımını, **bu projeye eklenecek bir `Category` entity'si** üzerinden göstermek en doğru yaklaşım olur.

### Senaryo

- Bir `Category`'nin birden fazla `Book`'u olabilir.
- Bir `Book` sadece bir `Category`'ye ait olsun.

Yani:

- **Category 1 -> N Book**

### 11.1 Category entity ekleme

**Yeni dosya örneği:** `Entities/Models/Category.cs`

```csharp
namespace Entities.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
```

### 11.2 Book entity'yi güncelleme

**Dosya:** `Entities/Models/Book.cs`

```csharp
namespace Entities.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
```

### 11.3 DbContext'e DbSet ekleme

**Dosya:** `Repositories/EFCore/RepositoryContext.cs`

```csharp
public DbSet<Book> Books { get; set; }
public DbSet<Category> Categories { get; set; }
```

### 11.4 Fluent API ile ilişki tanımlama

İlişkiyi `OnModelCreating` içinde veya ayrı config sınıfında tanımlayabiliriz.

**Örnek:**

```csharp
modelBuilder.Entity<Book>()
    .HasOne(b => b.Category)
    .WithMany(c => c.Books)
    .HasForeignKey(b => b.CategoryId);
```

Buradaki anlam:

- `Book`, bir `Category`'ye sahiptir.
- `Category`, birçok `Book` içerir.
- Foreign key: `CategoryId`

### 11.5 Migration oluşturma

Bu değişiklikten sonra:

```bash
dotnet ef migrations add AddCategoryRelation --project WebApi --startup-project WebApi --context RepositoryContext
dotnet ef database update --project WebApi --startup-project WebApi --context RepositoryContext
```

---

## 12) Include nasıl kullanılır?

`Include`, ilişkili veriyi de aynı sorguda getirmek için kullanılır.

Şu anda projede navigation property olmadığı için `Include` kullanılmıyor.
Ben de arama yaptığımda projede `Include(...)` veya `ThenInclude(...)` kullanımına rastlanmadı.

Ama yukarıdaki `Category` ilişkisini eklediğimizi düşünelim.

### Örnek: Kitapları kategorileri ile birlikte çekmek

**BookRepository içinde olabilecek örnek:**

```csharp
using Microsoft.EntityFrameworkCore;

public IQueryable<Book> GetAllBooksWithCategory(bool trackChanges) =>
    FindAll(trackChanges)
        .Include(b => b.Category)
        .OrderBy(b => b.Id);
```

Bu sorgu sayesinde her `Book` ile beraber onun `Category` verisi de gelir.

### Tek kitap + kategori örneği

```csharp
public Book GetOneBookWithCategory(int id, bool trackChanges) =>
    FindByCondition(b => b.Id == id, trackChanges)
        .Include(b => b.Category)
        .SingleOrDefault();
```

---

## 13) ThenInclude nasıl kullanılır?

`ThenInclude`, bir ilişkiden sonra onun alt ilişkisini yüklemek için kullanılır.

Örnek senaryo:

- `Category` -> birçok `Book`
- `Book` -> bir `Publisher`

Bu durumda önce `Books`, sonra onların `Publisher` bilgisi çekilebilir.

### Örnek model fikri

```csharp
public class Publisher
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Book> Books { get; set; }
}
```

```csharp
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public decimal Price { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; }

    public int PublisherId { get; set; }
    public Publisher Publisher { get; set; }
}
```

### ThenInclude örneği

```csharp
using Microsoft.EntityFrameworkCore;

var categories = _context.Categories
    .Include(c => c.Books)
    .ThenInclude(b => b.Publisher)
    .ToList();
```

Anlamı:

1. Önce `Category` getir.
2. Sonra o category'nin `Books` koleksiyonunu getir.
3. Sonra her book için `Publisher` bilgisini getir.

---

## 14) API / Controller / Service / Repository birlikte nasıl çalışıyor?

Bu proje katmanlı mimari kullandığı için akış nettir.

### İstek akışı

Örnek istek:

```http
GET /api/books/1
```

Akış:

1. `Presentation/Controllers/BooksController.cs`
2. `Services/BookManager.cs`
3. `Repositories/EFCore/BookRepository.cs`
4. `Repositories/EFCore/RepositoryBase.cs`
5. `Repositories/EFCore/RepositoryContext.cs`
6. PostgreSQL

### Controller örneği

```csharp
var book = _manager.BookService.GetOneBookById(id, false);
return Ok(book);
```

### Service örneği

```csharp
var book = _manager.Book.GetOneBookById(id, trackChanges);
if (book is null)
    throw new BookNotFoundException(id);
return book;
```

### Repository örneği

```csharp
FindByCondition(b => b.Id.Equals(id), trackChanges)
    .SingleOrDefault();
```

Bu ayrımın avantajı:

- Controller sade kalır.
- İş kuralları service'te toplanır.
- Veri erişimi repository'de toplanır.
- EF Core detayları API katmanına sızmaz.

---

## 15) Swagger üzerinden test nasıl yapılır?

### 15.1 Swagger projede açık mı?

Evet.

**Dosya:** `WebApi/Program.cs`

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Yani uygulama `Development` ortamında çalışıyorsa Swagger aktif.

### 15.2 Launch ayarları

**Dosya:** `WebApi/Properties/launchSettings.json`

```json
"applicationUrl": "https://localhost:7222;http://localhost:5222",
"launchUrl": "swagger",
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development"
}
```

Bu da şunu söyler:

- Proje çalışınca tarayıcı `swagger` sayfasına gider.
- URL'ler:
  - `https://localhost:7222/swagger`
  - `http://localhost:5222/swagger`

### 15.3 Uygulamayı çalıştırma

Çözüm kökünden:

```bash
dotnet run --project WebApi
```

### 15.4 Swagger'da test edilecek endpointler

Bu projede controller route'u:

```csharp
[Route("api/books")]
```

Yani endpointler:

- `GET /api/books`
- `GET /api/books/{id}`
- `POST /api/books`
- `PUT /api/books/{id}`
- `DELETE /api/books/{id}`
- `PATCH /api/books/{id}`

---

## 16) Swagger test senaryoları

### 16.1 GET all

Swagger'da:

- `GET /api/books`
- **Try it out**
- **Execute**

Beklenen: tüm kitap listesi döner.

### 16.2 GET by id

- `GET /api/books/{id}`
- `id = 1`
- **Execute**

Beklenen: `Id = 1` olan kitap döner.

### 16.3 POST ile yeni kitap ekleme

- `POST /api/books`
- Body örneği:

```json
{
  "id": 10,
  "title": "EF Core Öğreniyorum",
  "price": 250
}
```

Beklenen:

- `201 Created`
- Eklenen kitap response'ta döner.

### 16.4 PUT ile kitap güncelleme

- `PUT /api/books/{id}`
- `id = 10`
- Body:

```json
{
  "id": 10,
  "title": "EF Core İleri Seviye",
  "price": 300
}
```

Beklenen:

- `204 No Content`

### 16.5 DELETE ile kayıt silme

- `DELETE /api/books/{id}`
- `id = 10`
- **Execute**

Beklenen:

- `204 No Content`

### 16.6 PATCH ile kısmi güncelleme

Bu projede `JsonPatchDocument<Book>` kullanılıyor.

- `PATCH /api/books/{id}`
- `id = 1`
- Body örneği:

```json
[
  {
    "op": "replace",
    "path": "/title",
    "value": "Yeni Kitap Adı"
  },
  {
    "op": "replace",
    "path": "/price",
    "value": 499
  }
]
```

Beklenen:

- `204 No Content`

---

## 17) Bu proje özelinde kısa özet

Bu çözümde EF Core şu amaçlarla kullanılıyor:

- `Book` entity'sini tablo gibi yönetmek
- PostgreSQL'e bağlanmak
- Repository pattern ile CRUD yapmak
- `trackChanges` ve `AsNoTracking()` ile okuma/güncelleme davranışını ayırmak
- Migration ile veritabanı değişimlerini yönetmek
- Swagger ile API endpointlerini test etmek

### En önemli mevcut dosyalar

- `Entities/Models/Book.cs`
- `Repositories/EFCore/RepositoryContext.cs`
- `Repositories/EFCore/RepositoryBase.cs`
- `Repositories/EFCore/BookRepository.cs`
- `Services/BookManager.cs`
- `Presentation/Controllers/BooksController.cs`
- `WebApi/Extensions/ServicesExtensions.cs`
- `WebApi/ContextFactory/RepositoryContextFactory.cs`
- `WebApi/Program.cs`

---

## 18) Son cümle

Eğer istersen bir sonraki adımda ben bu projeye gerçekten:

- `Category` entity'si ekleyip,
- `Book -> Category` one-to-many ilişkisini kurup,
- `Include` / `ThenInclude` kullanan gerçek repository metotlarını yazıp,
- migration komutlarını da netleştirerek

örnek yapıyı doğrudan kod üzerinde de oluşturabilirim.