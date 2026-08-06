using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderApp.API.Infrastructure.Persistence;

namespace OrderApp.Tests.Helpers;

/// <summary>
/// Her test için izole, bellekte çalışan bir AppDbContext örneği üretir.
/// Ayrı veritabanı adı kullanarak testlerin birbirini etkilemesini önler.
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Benzersiz isimli bir InMemory veritabanı döner.
    /// Her çağrıda temiz bir DB elde edilir.
    /// InMemory provider transaction'ı desteklemediğinden uyarı suppress edilir.
    /// </summary>
    public static AppDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Test süresince geçerli, gerçek bir IMemoryCache örneği döner.
    /// Moq yerine gerçek implementasyon kullanılır — cache davranışını
    /// olduğu gibi test etmek için daha güvenilir.
    /// </summary>
    public static IMemoryCache CreateCache()
    {
        var options = Options.Create(new MemoryCacheOptions());
        return new MemoryCache(options);
    }
}
