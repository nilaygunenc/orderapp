using Microsoft.EntityFrameworkCore;
using OrderApp.API.Application.Interfaces;
using OrderApp.API.Application.Services;
using OrderApp.API.Infrastructure.Middleware;
using OrderApp.API.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------ //
//  1. Veritabanı — SQLite + EF Core
// ------------------------------------------------------------------ //
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ------------------------------------------------------------------ //
//  2. Caching
// ------------------------------------------------------------------ //
// Cache'in kullanıcı tarafından üretilen arama anahtarlarıyla sınırsız büyümesini
// engelle. Sınır, yaklaşık olarak cache'lenen ürün DTO adedi üzerinden ölçülür.
builder.Services.AddMemoryCache(options => options.SizeLimit = 1_000);

// ------------------------------------------------------------------ //
//  3. Uygulama servisleri — Dependency Injection
// ------------------------------------------------------------------ //
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// ------------------------------------------------------------------ //
//  4. Controller'lar
//     - Validation hataları otomatik olarak ProblemDetails formatında
//       dönsün (SuppressModelStateInvalidFilter = false, default).
// ------------------------------------------------------------------ //
builder.Services.AddControllers();

// ------------------------------------------------------------------ //
//  5. Swagger / OpenAPI
// ------------------------------------------------------------------ //
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "OrderApp API",
        Version = "v1",
        Description = "Mini Sipariş Uygulaması REST API"
    });

    // XML comment'lerini Swagger UI'a dahil et
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ------------------------------------------------------------------ //
//  6. CORS — React uygulaması için (dev: localhost:5173)
// ------------------------------------------------------------------ //
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
        policy
            .WithOrigins(
                "http://localhost:5173",  // Vite dev server
                "http://localhost:3000")  // CRA fallback
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// ------------------------------------------------------------------ //

var app = builder.Build();

// ------------------------------------------------------------------ //
//  7. Global exception handler — tüm pipeline'ı sarar, ilk sıraya alınır
// ------------------------------------------------------------------ //
app.UseGlobalExceptionHandler();

// ------------------------------------------------------------------ //
//  8. Migration uygula + seed data yükle (uygulama ilk açılışında)
// ------------------------------------------------------------------ //
await ApplyMigrationsAsync(app);

// ------------------------------------------------------------------ //
//  9. HTTP pipeline
// ------------------------------------------------------------------ //
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderApp API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.MapControllers();

app.Run();

// ------------------------------------------------------------------ //
//  Yardımcı: Migration + seed
// ------------------------------------------------------------------ //
static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Pending migration varsa uygula, yoksa EnsureCreated ile tablolar oluştur
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Migration uygulandı.");
        }
        else
        {
            // Migration sistemi çalışmadıysa (ör: ilk kurulum) tabloları doğrudan oluştur
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("Veritabanı hazır.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Veritabanı başlatılırken hata oluştu.");
        throw;
    }
}

// Test projesi Program.cs'e erişebilsin
public partial class Program { }
