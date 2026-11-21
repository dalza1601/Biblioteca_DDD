using Catalogo.Api.Contracts;
using Catalogo.Application.Contracts;
using Catalogo.Application.Services;

namespace Catalogo.Api.Endpoints;

/// <summary>
/// Endpoints HTTP del catálogo. Solo llaman al servicio de aplicación;
/// los errores los traduce ManejoExcepcionesMiddleware.
/// Cada endpoint recibe el CancellationToken de la petición y lo propaga hasta el repositorio.
/// </summary>
public static class LibrosEndpoints
{
    public static void MapLibrosEndpoints(this IEndpointRouteBuilder app)
    {
        var libros = app.MapGroup("/libros").WithTags("Libros");

        libros.MapGet("/", async (ServicioLibros servicio, CancellationToken cancellationToken) =>
            Results.Ok(await servicio.ListarAsync(cancellationToken)))
            .WithName("ListarLibros");

        libros.MapGet("/{id:int}", async (int id, ServicioLibros servicio, CancellationToken cancellationToken) =>
        {
            var libro = await servicio.ObtenerPorIdAsync(id, cancellationToken);
            return Results.Ok(new LibroResponse(libro.Id, libro.Codigo, libro.Titulo, libro.Activo));
        })
        .WithName("ObtenerLibro");

        libros.MapPost("/", async (RegistrarLibroComando comando, ServicioLibros servicio, CancellationToken cancellationToken) =>
        {
            var libro = await servicio.RegistrarAsync(comando, cancellationToken);
            return Results.Created($"/libros/{libro.Id}", libro);
        })
        .WithName("RegistrarLibro");

        libros.MapPost("/{id:int}/desactivar", async (int id, ServicioLibros servicio, CancellationToken cancellationToken) =>
            Results.Ok(await servicio.DesactivarAsync(id, cancellationToken)))
            .WithName("DesactivarLibro");
    }
}
