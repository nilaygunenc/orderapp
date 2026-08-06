namespace OrderApp.API.Application.Common;

/// <summary>
/// Uygulama genelinde kullanılan cache key sabitleri.
/// Magic string kullanımını önler, tutarlılığı sağlar.
/// </summary>
public static class CacheKeys
{
    private static CancellationTokenSource _productExpiration = new();

    public const string ProductList = "products:all";

    /// <summary>
    /// Arama terimi normalize edilir: trim + lowercase.
    /// "Klavye", "klavye", " klavye " → aynı cache key.
    /// </summary>
    public static string ProductSearch(string term)
        => $"products:search:{NormalizeTerm(term)}";

    public static string ProductDetail(int id)
        => $"products:detail:{id}";

    /// <summary>Cache TTL — 10 dakika absolute expiration.</summary>
    public static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(10);

    /// <summary>Tüm ürün cache kayıtlarını aynı anda geçersiz kılan token.</summary>
    public static CancellationToken ProductExpirationToken
        => Volatile.Read(ref _productExpiration).Token;

    public static void InvalidateProducts()
    {
        var previous = Interlocked.Exchange(
            ref _productExpiration,
            new CancellationTokenSource());
        previous.Cancel();
    }

    // ------------------------------------------------------------------ //

    /// <summary>
    /// Arama terimi normalizasyonu — hem cache key hem DB sorgusu aynı
    /// değeri kullanmalı. LIKE sorgusu case-insensitive (SQLite default),
    /// cache key de lowercase olmalı ki "Klavye" ve "klavye" aynı cache'e düşsün.
    /// </summary>
    internal static string NormalizeTerm(string term)
        => term.Trim().ToLowerInvariant();
}
