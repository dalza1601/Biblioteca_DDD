using Prestamos.Domain;

namespace Prestamos.Api;

public static class PrestamosEndpoints
{
    public static void MapPrestamosEndpoints(this IEndpointRouteBuilder app)
    {
        var prestamos = app.MapGroup("/prestamos").WithTags("Prestamos");

        prestamos.MapGet("/", (RepositorioPrestamos repositorio) =>
            Results.Ok(repositorio.Listar().Select(ARespuesta)))
            .WithName("ListarPrestamos");

        prestamos.MapGet("/{id:int}", (int id, RepositorioPrestamos repositorio) =>
            repositorio.ObtenerPorId(id) is { } prestamo
                ? Results.Ok(ARespuesta(prestamo))
                : ManejoErrores.NoEncontrado(id))
            .WithName("ObtenerPrestamo");

        prestamos.MapPost("/", (RegistrarPrestamoRequest request, RepositorioPrestamos repositorio) =>
        {
            if (request.FechaInicio is not { } fechaInicio)
                return ManejoErrores.Error(StatusCodes.Status400BadRequest, "La fecha de inicio es obligatoria.");

            if (request.FechaLimite is not { } fechaLimite)
                return ManejoErrores.Error(StatusCodes.Status400BadRequest, "La fecha límite es obligatoria.");

            var prestamo = Prestamo.Registrar(
                request.LibroId ?? 0, // el dominio rechaza 0: "LibroId no puede ser nulo o 0"
                CodigoLector.Crear(request.Lector),
                fechaInicio,
                fechaLimite);

            repositorio.Agregar(prestamo);

            return Results.Created($"/prestamos/{prestamo.Id}", ARespuesta(prestamo));
        })
        .WithName("RegistrarPrestamo");

        prestamos.MapPost("/{id:int}/devolver", (int id, DevolverPrestamoRequest request, RepositorioPrestamos repositorio) =>
        {
            if (repositorio.ObtenerPorId(id) is not { } prestamo)
                return ManejoErrores.NoEncontrado(id);

            if (request.FechaDevolucion is not { } fechaDevolucion)
                return ManejoErrores.Error(StatusCodes.Status400BadRequest, "La fecha de devolución es obligatoria.");

            prestamo.Devolver(fechaDevolucion);
            return Results.Ok(ARespuesta(prestamo));
        })
        .WithName("DevolverPrestamo");
    }

    private static PrestamoResponse ARespuesta(Prestamo prestamo) =>
        new(
            prestamo.Id,
            prestamo.LibroId,
            prestamo.Lector.Valor,
            prestamo.FechaInicio,
            prestamo.FechaLimite,
            prestamo.FechaDevolucion,
            prestamo.Estado.ToString());
}
