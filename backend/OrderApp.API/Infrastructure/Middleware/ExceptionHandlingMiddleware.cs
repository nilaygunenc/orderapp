using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OrderApp.API.Application.Exceptions;

namespace OrderApp.API.Infrastructure.Middleware;

/// <summary>
/// Uygulama genelinde fırlatılan tüm exception'ları yakalar ve
/// RFC 7807 standardına uygun ProblemDetails formatında döner.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlenmeyen exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problem = exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Kaynak Bulunamadı",
                Detail = notFound.Message,
                Instance = context.Request.Path
            },

            BusinessException business => BuildBusinessProblem(context, business),

            // 400 — validation veya argüman hataları
            ArgumentException argEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Geçersiz İstek",
                Detail = argEx.Message,
                Instance = context.Request.Path
            },

            // 500 — beklenmeyen hatalar (detay client'a sızdırılmaz)
            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Sunucu Hatası",
                Detail = "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.",
                Instance = context.Request.Path
            }
        };

        context.Response.StatusCode = problem.Status!.Value;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
    }

    /// <summary>
    /// BusinessException için birden fazla hata mesajını
    /// ProblemDetails.Extensions["errors"] alanına ekler.
    /// </summary>
    private static ProblemDetails BuildBusinessProblem(HttpContext context, BusinessException ex)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "İş Kuralı İhlali",
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        // Tek hata olduğunda da ürün/stok ayrıntısını kaybetme.
        problem.Extensions["errors"] = ex.Errors;

        return problem;
    }
}
