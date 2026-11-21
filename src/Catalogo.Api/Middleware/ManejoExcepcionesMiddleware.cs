using Catalogo.Api.Contracts;
using Catalogo.Application.Exceptions;
using Catalogo.Domain.Exceptions;
using Microsoft.AspNetCore.WebUtilities;

namespace Catalogo.Api.Middleware;

/// <summary>
/// Middleware único de errores: traduce las excepciones a códigos HTTP y garantiza que
/// toda respuesta de error tenga la forma {"error": "mensaje"}, incluidas las que genera
/// el propio framework sin cuerpo (ruta inexistente, método no permitido, etc.).
/// </summary>
public sealed class ManejoExcepcionesMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejoExcepcionesMiddleware> _logger;

    public ManejoExcepcionesMiddleware(RequestDelegate siguiente, ILogger<ManejoExcepcionesMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _siguiente(contexto);
        }
        catch (OperationCanceledException) when (contexto.RequestAborted.IsCancellationRequested)
        {
            // El cliente canceló la petición (CancellationToken): no hay a quién responder
            // y no es un error del servidor, así que no se registra como 500.
            return;
        }
        catch (Exception excepcion) when (!contexto.Response.HasStarted)
        {
            var (statusCode, mensaje) = Traducir(excepcion);

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(excepcion, "Error no controlado.");

            await EscribirError(contexto, statusCode, mensaje);
            return;
        }

        if (EsErrorSinCuerpo(contexto.Response))
        {
            var mensaje = contexto.Response.StatusCode == StatusCodes.Status404NotFound
                ? "Recurso no encontrado."
                : ReasonPhrases.GetReasonPhrase(contexto.Response.StatusCode);

            await EscribirError(contexto, contexto.Response.StatusCode, mensaje);
        }
    }

    private static (int StatusCode, string Mensaje) Traducir(Exception excepcion) => excepcion switch
    {
        ValidacionDominioException e => (StatusCodes.Status400BadRequest, e.Message),
        RecursoNoEncontradoException e => (StatusCodes.Status404NotFound, e.Message),
        ReglaNegocioException e => (StatusCodes.Status409Conflict, e.Message),
        ConflictoException e => (StatusCodes.Status409Conflict, e.Message),
        BadHttpRequestException e => (e.StatusCode, "La solicitud no es válida. Revisa el formato del cuerpo JSON."),
        _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
    };

    private static bool EsErrorSinCuerpo(HttpResponse respuesta) =>
        respuesta.StatusCode >= 400
        && !respuesta.HasStarted
        && respuesta.ContentLength is null
        && string.IsNullOrEmpty(respuesta.ContentType);

    private static Task EscribirError(HttpContext contexto, int statusCode, string mensaje)
    {
        contexto.Response.Clear();
        contexto.Response.StatusCode = statusCode;
        return contexto.Response.WriteAsJsonAsync(new ErrorResponse(mensaje));
    }
}
