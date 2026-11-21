using Prestamos.Api.Aplicacion;
using Prestamos.Application.Contracts;
using Prestamos.Application.Services;

namespace Prestamos.Api.Endpoints;

/// <summary>
/// Endpoints HTTP de préstamos. Solo llaman al servicio de aplicación;
/// los errores los traduce ManejoExcepcionesMiddleware.
/// </summary>
public static class PrestamosEndpoints
{
    public static void MapPrestamosEndpoints(this IEndpointRouteBuilder app)
    {
        var prestamos = app.MapGroup("/prestamos").WithTags("Prestamos");

        prestamos.MapGet("/", (ServicioPrestamos servicio) =>
            Results.Ok(servicio.Listar()))
            .WithName("ListarPrestamos");

        prestamos.MapGet("/{id:int}", (int id, ServicioPrestamos servicio) =>
            Results.Ok(servicio.ObtenerPorId(id)))
            .WithName("ObtenerPrestamo");

        // Solo llama al caso de uso y traduce su resultado a HTTP.
        prestamos.MapPost("/", async (RegistrarPrestamoComando comando, RegistrarPrestamo casoDeUso, CancellationToken ct) =>
            PrestamoHttp.Traducir(await casoDeUso.EjecutarAsync(comando, ct)))
        .WithName("RegistrarPrestamo");

        prestamos.MapPost("/{id:int}/devolver", (int id, DevolverPrestamoComando comando, ServicioPrestamos servicio) =>
            Results.Ok(servicio.Devolver(id, comando)))
            .WithName("DevolverPrestamo");
    }
}
