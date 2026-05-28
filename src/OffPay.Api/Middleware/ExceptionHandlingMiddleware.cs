using Microsoft.AspNetCore.Mvc;
using OffPay.Domain.Exceptions;

namespace OffPay.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (DispositivoNaoEncontradoException ex)
        {
            await EscreverProblemDetails(context, StatusCodes.Status404NotFound, "Recurso nao encontrado", ex.Message);
        }
        catch (DispositivoInativoException ex)
        {
            await EscreverProblemDetails(context, StatusCodes.Status403Forbidden, "Acesso negado", ex.Message);
        }
        catch (LogAuditoriaNaoEncontradoException ex)
        {
            await EscreverProblemDetails(context, StatusCodes.Status404NotFound, "Recurso nao encontrado", ex.Message);
        }
        catch (DomainException ex)
        {
            await EscreverProblemDetails(context, StatusCodes.Status422UnprocessableEntity, "Regra de negocio violada", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar requisicao {Method} {Path}", context.Request.Method, context.Request.Path);
            await EscreverProblemDetails(context, StatusCodes.Status500InternalServerError, "Erro interno", "Ocorreu um erro inesperado.");
        }
    }

    private static async Task EscreverProblemDetails(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
