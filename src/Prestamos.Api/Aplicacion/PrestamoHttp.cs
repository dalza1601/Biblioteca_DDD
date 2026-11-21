using Prestamos.Api.Contracts;

namespace Prestamos.Api.Aplicacion;

/// <summary>
/// Traduce el resultado del caso de uso RegistrarPrestamo a una respuesta HTTP.
/// Los errores siempre llevan el cuerpo { "error": "..." }.
/// </summary>
public static class PrestamoHttp
{
    public const string EncabezadoEventoId = "X-Event-Id";
    public const string EncabezadoEventoPublicado = "X-Event-Published";

    public static IResult Traducir(ResultadoRegistrarPrestamo resultado) => resultado switch
    {
        // 201 + encabezados con el id del evento y si llegó a RabbitMQ.
        ResultadoRegistrarPrestamo.Creado creado => new ResultadoConEncabezados(
            Results.Created($"/prestamos/{creado.Prestamo.Id}", creado.Prestamo),
            new Dictionary<string, string>
            {
                [EncabezadoEventoId] = creado.EventoId.ToString(),
                [EncabezadoEventoPublicado] = creado.EventoPublicado ? "true" : "false"
            }),

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
