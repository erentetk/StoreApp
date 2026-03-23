# EF Core Rehberi - bsStoreApp Projesi Üzerinden
İlgili projede Ef core kullanımı  gösterilmiştir.


## 1) EF Core nedir?

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

## 2) Bu proje Code First mi, Database First mü?

Bu proje mantık olarak **Code First** yaklaşımına daha yakındır.

### Code First mantığı

Önce kod yazılır:

- Entity sınıfları oluşturulur.
- `DbContext` yazılır.
- Fluent API / Config sınıfları tanımlanır.
- Sonra migration ile veritabanı oluşturulur veya güncellenir.

Bu projede Code First olduğunu gösteren parçalar:

   - `Entities/Models/Book.cs`

   - `Repositories/EFCore/RepositoryContext.cs`

**config/seed**
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
**>** Burası veri tabanına başlangıç datayı verir
```

Yani önce sınıf tanımlanıyor, sonra veritabanı bu sınıflara göre şekilleniyor.

### Database First mantığı

Database First yaklaşımında süreç tersidir:

- Önce hazır bir veritabanı vardır.
- Sonra EF Core o veritabanından sınıf üretir.

## 3) Migration

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

ama Ama terminali `WebApi` klasörünün içinde açarsan, o zaman kısa komut çoğu durumda yeterli olur:
```bash
`dotnet ef migrations add InitialCreate`
```


### Migration ne zaman kullanılır?

- Yeni tablo eklerken
- Alan eklerken / silerken
- İlişki eklerken
- Seed data yapısı değiştiğinde
- Kısaca veri tabanaı ile alakalı bir güncelleme yapılınca
---

## 4) Veritabanı bağlantısı bu projede nasıl kurulmuş?

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
  "sqlConnection": "Host=localhost;Port=5432;Database=bsStoreApp;Username=postgres;Password=erenDbSifre;",
  "sqlConnectionSqlserver": "Data Source = (localdb)\\MSSQLLocalDB; Initial Catalog = bsStoreApp; Integrated Security=true;"
}
```

---

## 5) CRUD işlemleri bu projede nasıl yapılıyor?

CRUD =

- **C**reate
- **R**ead
- **U**pdate
- **D**elete

Bu projede akış şu şekilde:

**Controller -> Service -> Repository -> EF Core(DbContext) -> Database**

Yani **BooksController**, doğrudan `DbContext` ile konuşmuyor.
Önce service'e gidiyor, service repository'yi çağırıyor, repository de EF Core'u kullanıyor.

---

## 6) Repository katmanındaki EF Core kullanımı

### 6.1 Generic repository tabanı

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

## 7) Book repository içindeki CRUD örnekleri

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


## 8) Service katmanında CRUD mantığı

**Dosya:** `Services/BookManager.cs`

### 8.1 Create

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

### 8.2 Read - Tüm kitapları getirme

```csharp
public IEnumerable<Book> GetAllBooks(bool trackChanges)
{
    return _manager.Book.GetAllBooks(trackChanges);
}
```

### 8.3 Read - Tek kitap getirme

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

### 8.4 Update

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

### 8.5 Delete

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

## 9) Controller katmanında CRUD kullanımı

**Dosya:** `Presentation/Controllers/BooksController.cs`

### 9.1 GET all

```csharp
[HttpGet]
public IActionResult GetAllBooks()
{
    var books = _manager.BookService.GetAllBooks(false);
    return Ok(books);
}
```

### 9.2 GET by id

```csharp
[HttpGet("{id:int}")]
public IActionResult GetOneBook([FromRoute(Name = "id")] int id)
{
    var book = _manager.BookService.GetOneBookById(id, false);
    return Ok(book);
}
```

### 9.3 POST

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

### 9.4 PUT

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

### 9.5 DELETE

```csharp
[HttpDelete("{id:int}")]
public IActionResult DeleteOneBook([FromRoute(Name = "id")] int id)
{
    _manager.BookService.DeleteOneBook(id, false);
    return NoContent();
}
```

### 9.6 PATCH

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
- Book, bir Category'ye sahiptir.
- Category, birçok Book içerir.
- Foreign key: CategoryId

### 11.5 Migration oluşturma

Bu değişiklikten sonra:

```bash
dotnet ef migrations add AddCategoryRelation --project WebApi --startup-project WebApi --context RepositoryContext
dotnet ef database update --project WebApi --startup-project WebApi --context RepositoryContext
```

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
yada IDE üzerinden 

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


