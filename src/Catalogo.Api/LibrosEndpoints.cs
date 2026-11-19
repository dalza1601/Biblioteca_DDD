using Catalogo.Domain;

namespace Catalogo.Api;

public static class LibrosEndpoints
{
    public static void MapLibrosEndpoints(this IEndpointRouteBuilder app)
    {
        var libros = app.MapGroup("/libros").WithTags("Libros");

        libros.MapGet("/", (RepositorioLibros repositorio) =>
            Results.Ok(repositorio.Listar().Select(ARespuesta)))
            .WithName("ListarLibros");

        libros.MapGet("/{id:int}", (int id, RepositorioLibros repositorio) =>
            repositorio.ObtenerPorId(id) is { } libro
                ? Results.Ok(ARespuesta(libro))
                : ManejoErrores.NoEncontrado(id))
            .WithName("ObtenerLibro");

        libros.MapPost("/", (RegistrarLibroRequest request, RepositorioLibros repositorio) =>
        {
            var codigo = CodigoLibro.Crear(request.Codigo);

            // Se valida antes de crear la entidad para no consumir un Id en un registro rechazado.
            if (repositorio.ExisteCodigo(codigo))
                throw new ReglaNegocioException($"Ya existe un libro con el código {codigo}.");

            var libro = Libro.Crear(codigo, request.Titulo);
            repositorio.Agregar(libro);

            return Results.Created($"/libros/{libro.Id}", ARespuesta(libro));
        })
        .WithName("RegistrarLibro");

        libros.MapPost("/{id:int}/desactivar", (int id, RepositorioLibros repositorio) =>
        {
            if (repositorio.ObtenerPorId(id) is not { } libro)
                return ManejoErrores.NoEncontrado(id);

            libro.Desactivar();
            return Results.Ok(ARespuesta(libro));
        })
        .WithName("DesactivarLibro");
    }

    private static LibroResponse ARespuesta(Libro libro) =>
        new(libro.Id, libro.Codigo.Valor, libro.Titulo, libro.Activo);
}
