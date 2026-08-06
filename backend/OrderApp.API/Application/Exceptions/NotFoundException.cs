namespace OrderApp.API.Application.Exceptions;

/// <summary>
/// Veritabanında aranılan kayıt bulunamadığında fırlatılır.
/// Global handler bunu 404 Not Found olarak dönüştürür.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object key)
        : base($"'{resourceName}' kaynağı '{key}' anahtarıyla bulunamadı.")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
