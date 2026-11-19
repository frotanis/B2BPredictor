using System.Net;

namespace B2BPredictor.UI.Services;

/// <summary>Помилка, повернута API, з уже підготовленим для користувача текстом.</summary>
public sealed class ApiException : Exception
{
    public ApiException()
    {
    }

    public ApiException(string message)
        : base(message)
    {
    }

    public ApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ApiException(HttpStatusCode? statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode? StatusCode { get; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
}
