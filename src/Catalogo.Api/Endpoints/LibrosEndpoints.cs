using Catalogo.Application.Contracts;
using Catalogo.Application.Services;

namespace Catalogo.Api.Endpoints;

/// <summary>
/// Endpoints HTTP del catálogo. Solo llaman al servicio de aplicación;
/// los errores los traduce ManejoExcepcionesMiddleware.
/// </summary>
public static class LibrosEndpoints
{
    public static void MapLibrosEndpoints(this IEndpointRouteBuilder app)
    {
        var libros = app.MapGroup("/libros").WithTags("Libros");

        libros.MapGet("/", (ServicioLibros servicio) =>
            Results.Ok(servicio.Listar()))
            .WithName("ListarLibros");

        libros.MapGet("/{id:int}", (int id, ServicioLibros servicio) =>
            Results.Ok(servicio.ObtenerPorId(id)))
            .WithName("ObtenerLibro");

        libros.MapPost("/", (RegistrarLibroComando comando, ServicioLibros servicio) =>
        {
            var libro = servicio.Registrar(comando);
            return Results.Created($"/libros/{libro.Id}", libro);
        })
        .WithName("RegistrarLibro");

        libros.MapPost("/{id:int}/desactivar", (int id, ServicioLibros servicio) =>
            Results.Ok(servicio.Desactivar(id)))
            .WithName("DesactivarLibro");
    }
}
