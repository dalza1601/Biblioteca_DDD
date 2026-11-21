using Prestamos.Api.Contracts;

namespace Prestamos.Api.Aplicacion;

/// <summary>
/// Traduce el resultado del caso de uso RegistrarPrestamo a una respuesta HTTP.
/// Los errores siempre llevan el cuerpo { "error": "..." }.
/// </summary>
public static class PrestamoHttp
{
    public static IResult Traducir(ResultadoRegistrarPrestamo resultado) => resultado switch
    {
        ResultadoRegistrarPrestamo.Creado creado =>
            Results.Created($"/prestamos/{creado.Prestamo.Id}", creado.Prestamo),

        ResultadoRegistrarPrestamo.LibroNoDisponible r =>
            Error(StatusCodes.Status409Conflict, $"El libro {r.LibroId} no está disponible para préstamo."),

        ResultadoRegistrarPrestamo.LibroNoExiste r =>
            Error(StatusCodes.Status422UnprocessableEntity, $"El libro {r.LibroId} no existe en Catálogo."),

        ResultadoRegistrarPrestamo.CatalogoNoDisponible =>
            Error(StatusCodes.Status503ServiceUnavailable, "El servicio de Catálogo no está disponible. Intenta más tarde."),

        ResultadoRegistrarPrestamo.CatalogoTimeout =>
            Error(StatusCodes.Status504GatewayTimeout, "El servicio de Catálogo no respondió a tiempo."),

        _ => throw new InvalidOperationException($"Resultado no contemplado: {resultado.GetType().Name}.")
    };

    private static IResult Error(int statusCode, string mensaje) =>
        Results.Json(new ErrorResponse(mensaje), statusCode: statusCode);
}
