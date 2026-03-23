# SOLID - bsStoreApp

## 2. Projenin Genel Mimari Yapısı

Proje, klasik katmanlı bir Web API yapısı kullanmakta:

- **Presentation** -> Controller katmanı
- **Services** -> İş kuralları / servis katmanı
- **Repositories** -> Veri erişim katmanı
- **Entities** -> Entity, DTO ve exception modelleri
- **WebApi** -> Uygulama başlangıcı, DI ve middleware konfigürasyonu

Bu yapı, SOLID prensiplerinin öne çıktıg yerler:

- controller ile iş mantığının ayrılması,
- repository abstraction kullanılması,
- dependency injection kullanılması,
- hata yönetimi için middleware eklenmesi

## 3. S - Single Responsibility Principle

Bir sınıfın değişmek için **tek bir nedeni** olmalıdır.
 
#### 3.1 `LoggerManager`

Bu sınıfın temel sorumluluğu yalnızca loglama işlemleridir.

```csharp
public class LoggerManager : ILoggerService
{
    private static ILogger logger = LogManager.GetCurrentClassLogger();
    public void LogDebug(string message) => logger.Debug(message);
    public void LogError(string message) => logger.Error(message);
    public void LogInfo(string message) => logger.Info(message);
    public void LogWarning(string message) => logger.Warn(message);
}
```

#### 3.2 `BookConfig`

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

Bu sınıfın görevi yalnızca EF Core entity konfigürasyonudur. Bu da SRP ile uyumludur.

#### 3.3 `ExceptionMiddlewareExtensions`

```csharp
public static void ConfigureExceptionHandlar(this WebApplication app,
    ILoggerService logger)
{
    app.UseExceptionHandler(appError =>
    {
        appError.Run(async context =>
        {
            // hata yakalama ve response üretimi
        });
    });
}
```

Bu kodun görevi merkezi hata yönetimidir. Bu da belirli ve tekil bir sorumluluktur.



`BookManager`, servis katmanında önemli bir sınıftır; ancak tek bir sorumluluk taşımadığı görülmektedir.

```csharp
public class BookManager : IBookService
{
    private readonly IRepositoryManager _manager;
    private readonly ILoggerService _logger;
    private readonly IMapper _mapper;

    public void UpdateOneBook(int id, BookDtoForUpdate bookDto, bool trackChanges)
    {
        var entity = _manager.Book.GetOneBookById(id, trackChanges);
        if (entity is null)
            throw new BookNotFoundException(id);

        entity = _mapper.Map<Book>(bookDto);

        _manager.Book.Update(entity);
        _manager.Save();
    }
}
```

Bu sınıfın içinde birden fazla sorumluluk bulunuyor:

- veri alma ve silme/güncelleme akışı,
- entity var mı kontrolü,
- exception fırlatma,
- DTO -> entity mapping,
- repository orchestration,
- transaction benzeri kayıt akışı (`Save`).

Bu nedenle `BookManager`, SRP açısından **kısmen problemli** bir örnektir.

## 4. O - Open/Closed Principle

Bir yazılım bileşeni **geliştirmeye açık**, **değişikliğe kapalı** olmalıdır.

### Örnek

#### 4.1 `RepositoryBase<T>` ve `BookRepository`

```csharp
public abstract class RepositoryBase<T> : IRepositoryBase<T>
    where T : class
{
    protected readonly RepositoryContext _context;

    public IQueryable<T> FindAll(bool trackChanges) =>
        !trackChanges ?
        _context.Set<T>().AsNoTracking() :
        _context.Set<T>();

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression,
        bool trackChanges) =>
        !trackChanges ?
        _context.Set<T>().Where(expression).AsNoTracking() :
        _context.Set<T>().Where(expression);
}
```

```csharp
public class BookRepository : RepositoryBase<Book>, IBookRepository
{
    public IQueryable<Book> GetAllBooks(bool trackChanges) =>
        FindAll(trackChanges).OrderBy(b => b.Id);

    public Book GetOneBookById(int id, bool trackChanges) =>
        FindByCondition(b => b.Id.Equals(id), trackChanges)
        .SingleOrDefault();
}
```

**CRUD davranışları `RepositoryBase<T>` içinde tanımlandı, `BookRepository` ise yalnızca kitaba özgü davranışı genişletmiştir.**

Benzer bir yapı yeni bir entity için de kullanılabilir:

- `AuthorRepository : RepositoryBase<Author>`
- `OrderRepository : RepositoryBase<Order>`

Bu yaklaşım tekrarları azaltır ve genişlemeyi kolaylaştırır.



## 5. L - Liskov Substitution Principle

Türetilmiş sınıflar, temel tiplerin yerine geçebilmeli ve sistemi bozmamalıdır.

Örnekler
#### 5.1 Exception hiyerarşisi

```csharp
public abstract class NotFoundException : Exception
{
    protected NotFoundException(string message) : base(message)
    {
    }
}
```

```csharp
public sealed class BookNotFoundException : NotFoundException
{
    public BookNotFoundException(int id)
        : base($"The book with id : {id} could not found .")
    {
    }
}
```

Bu yapı, `BookNotFoundException` nesnesinin `NotFoundException` olarak güvenle kullanılmasını sağlar.

Middleware tarafındaki kullanım da bunu destekler:

```csharp
context.Response.StatusCode = contextFeature.Error switch
{
    NotFoundException => StatusCodes.Status404NotFound,
    _ => StatusCodes.Status500InternalServerError
};
```

Burada alt sınıf olan `BookNotFoundException`, üst sınıf beklentisini bozmadan kullanılmakta

#### 5.2 Repository kalıtımı

```csharp
public class BookRepository : RepositoryBase<Book>, IBookRepository
{
}
```

`BookRepository`, `RepositoryBase<Book>` üzerine kurulu olduğu için temel repository davranışlarını bozmadan genişletmektedir. Bu da LSP ile uyumlu bir örnektir.


## 6. I - Interface Segregation Principle

İstemciler, kullanmadıkları metotlara bağımlı olmamalıdır.
Örnekler;
#### 6.1 `ILoggerService`

```csharp
public interface ILoggerService
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogDebug(string message);
}
```

Bu arayüz, loglama için gerekli küçük ve odaklı bir sözleşme sunmaktadır.

#### 6.2 `IBookService`

```csharp
public interface IBookService
{
    IEnumerable<Book> GetAllBooks(bool trackChanges);
    Book GetOneBookById(int id, bool trackChanges);
    Book CreateOneBook(Book book);
    void UpdateOneBook(int id, BookDtoForUpdate bookdto, bool trackChanges);
    void DeleteOneBook(int id, bool trackChanges);
}
```

Bu arayüz de yalnızca kitap işlemlerine odaklıdır. Controller, gereksiz metotları taşımayan bu servis sözleşmesini kullanmaktadır.

#### 6.3 `IBookRepository`

```csharp
public interface IBookRepository : IRepositoryBase<Book>
{
    IQueryable<Book> GetAllBooks(bool trackChanges);
    Book GetOneBookById(int id, bool trackChanges);
    void CreateOneBook(Book book);
    void UpdateOneBook(Book book);
    void DeleteOneBook(Book book);
}
```

Repository arayüzü de alan bazlı ve odaklıdır. Bu olumlu bir tasarım tercihidir.


## 7. D - Dependency Inversion Principle

Üst seviye modüller alt seviye modüllere değil, **soyutlamalara** bağımlı olmalıdır.

Örnekler;
#### 7.1 Controller'ın abstraction kullanması

```csharp
public class BooksController : ControllerBase
{
    private readonly IServiceManager _manager;

    public BooksController(IServiceManager manager)
    {
        _manager = manager;
    }
}
```

Controller doğrudan `BookManager` ya da `BookRepository` gibi somut sınıflara değil, `IServiceManager` abstraction'ına bağlıdır.

#### 7.2 Servisin abstraction kullanması

```csharp
public BookManager(IRepositoryManager manager,
    ILoggerService logger,
    IMapper mapper)
{
    _manager = manager;
    _logger = logger;
    _mapper = mapper;
}
```

Servis katmanı da doğrudan `RepositoryManager` veya `LoggerManager` gibi somut sınıflara değil, abstraction'lara bağlanmıştır.

#### 7.3 DI kayıtlarının merkezi olması

```csharp
public static void ConfigureRepositoryManager(this IServiceCollection services) =>
    services.AddScoped<IRepositoryManager, RepositoryManager>();

public static void ConfigureServiceManager(this IServiceCollection services) =>
    services.AddScoped<IServiceManager, ServiceManager>();

public static void ConfigureLoggerService(this IServiceCollection services) =>
    services.AddSingleton<ILoggerService, LoggerManager>();
```

Bu kullanım DIP açısından güçlü bir adımdır. Çünkü nesne oluşturma sorumluluğu merkezi konfigürasyona taşınmıştır.

## 8. Projeden Seçilmiş Kod Örnekleri ve Yorumlar

Bu bölümde projedeki bazı kod parçaları kısa yorumlarla birlikte özetlenmiştir.

### 8.1 Controller katmanı - doğru katman ayrımı, fakat entity bağımlılığı var

```csharp
[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IServiceManager _manager;

    public BooksController(IServiceManager manager)
    {
        _manager = manager;
    }
}
```

### 8.2 Service katmanı - orchestration iyi, sorumluluk yoğunluğu yüksek

```csharp
public Book GetOneBookById(int id, bool trackChanges)
{
    var book = _manager.Book.GetOneBookById(id,trackChanges);
    if (book is null)
         throw new BookNotFoundException(id);
    return book;
}
```

### 8.3 Middleware - çapraz kesen concern için doğru çözüm

```csharp
logger.LogError($"Something went wrong: {contextFeature.Error}");
await context.Response.WriteAsync(new ErrorDetails()
{
    StatusCode = context.Response.StatusCode,
    Message = contextFeature.Error.Message
}.ToString());
```

### 8.4 AutoMapper profili - mapping bilgisini ayrı yerde tutmak olumlu

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<BookDtoForUpdate, Book>();
    }
}
```
