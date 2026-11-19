using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using Prestamos.Domain;

namespace Prestamos.Api;

/// <summary>
/// Traduce las excepciones de dominio a códigos HTTP y garantiza que toda respuesta
/// de error tenga la forma {"error": "mensaje"}.
/// </summary>
public static class ManejoErrores
{
    public static IResult NoEncontrado(int id) =>
        Error(StatusCodes.Status404NotFound, $"No existe un préstamo con id {id}.");

    public static IResult Error(int statusCode, string mensaje) =>
        Results.Json(new ErrorResponse(mensaje), statusCode: statusCode);

    public static IServiceCollection AddManejoErrores(this IServiceCollection services) =>
        // Hace que los errores de lectura del body (JSON inválido, tipos incorrectos)
        // lleguen al manejador de excepciones también fuera de Development.
        services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = true);

    public static WebApplication UseManejoErrores(this WebApplication app)
    {
        app.UseExceptionHandler(manejador => manejador.Run(async contexto =>
        {
            var excepcion = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;

            var (statusCode, mensaje) = excepcion switch
            {
                ValidacionDominioException e => (StatusCodes.Status400BadRequest, e.Message),
                ReglaNegocioException e => (StatusCodes.Status409Conflict, e.Message),
                BadHttpRequestException e => (e.StatusCode, "La solicitud no es válida. Revisa el formato del cuerpo JSON."),
                _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                app.Logger.LogError(excepcion, "Error no controlado.");

            await Error(statusCode, mensaje).ExecuteAsync(contexto);
        }));

        // Errores sin cuerpo generados por el framework (ruta inexistente, método no permitido, etc.).
        app.UseStatusCodePages(async contexto =>
        {
            var respuesta = contexto.HttpContext.Response;
            var mensaje = respuesta.StatusCode == StatusCodes.Status404NotFound
                ? "Recurso no encontrado."
                : ReasonPhrases.GetReasonPhrase(respuesta.StatusCode);

            await respuesta.WriteAsJsonAsync(new ErrorResponse(mensaje));
        });

        return app;
    }
}
