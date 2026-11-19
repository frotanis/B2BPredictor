using B2BPredictor.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace B2BPredictor.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult ServiceProblem(string? message, ServiceErrorType errorType)
    {
        (int status, string title) = errorType switch
        {
            ServiceErrorType.Validation => (StatusCodes.Status400BadRequest, "Некоректний запит"),
            ServiceErrorType.NotFound => (StatusCodes.Status404NotFound, "Ресурс не знайдено"),
            ServiceErrorType.Conflict => (StatusCodes.Status409Conflict, "Конфлікт даних"),
            ServiceErrorType.Unauthorized => (StatusCodes.Status403Forbidden, "Доступ заборонено"),
            _ => (StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера"),
        };

        return Problem(detail: message, statusCode: status, title: title);
    }

    protected ObjectResult NotFoundProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status404NotFound, title: "Ресурс не знайдено");

    protected ObjectResult BadRequestProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest, title: "Некоректний запит");
}