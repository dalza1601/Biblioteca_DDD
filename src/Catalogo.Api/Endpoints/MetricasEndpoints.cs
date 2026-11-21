using Catalogo.Api.Contracts;
using Catalogo.Api.Mensajeria.Stores;

namespace Catalogo.Api.Endpoints;

/// <summary>
/// Métricas derivadas de los eventos prestamo.registrado consumidos desde RabbitMQ (solo en memoria).
/// </summary>
public static class MetricasEndpoints
{
    public static void MapMetricasEndpoints(this IEndpointRouteBuilder app)
    {
        var metricas = app.MapGroup("/metricas").WithTags("Metricas");

        metricas.MapGet("/prestamos-por-libro", (ContadorPrestamosPorLibro contador) =>
            Results.Ok(contador.Instantanea()
                .Select(par => new PrestamosPorLibroResponse(par.LibroId, par.Prestamos))))
            .WithName("PrestamosPorLibro");

        metricas.MapGet("/cuarentena", (Cuarentena cuarentena) =>
            Results.Ok(cuarentena.Listar()
                .Select(m => new MensajeEnCuarentenaResponse(m.FechaUtc, m.Motivo, m.Contenido))))
            .WithName("Cuarentena");
    }
}
