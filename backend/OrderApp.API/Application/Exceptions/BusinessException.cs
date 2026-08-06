namespace OrderApp.API.Application.Exceptions;

/// <summary>
/// Business kuralı ihlalinde fırlatılır (yetersiz stok, geçersiz miktar vb.).
/// Global handler bunu 422 Unprocessable Entity olarak dönüştürür.
/// </summary>
public class BusinessException : Exception
{
    /// <summary>Birden fazla hata mesajı döndürmek için kullanılır.</summary>
    public IReadOnlyList<string> Errors { get; }

    public BusinessException(string message) : base(message)
    {
        Errors = new List<string> { message };
    }

    public BusinessException(IEnumerable<string> errors) : base("Bir veya daha fazla iş kuralı ihlali oluştu.")
    {
        Errors = errors.ToList();
    }
}
