namespace Catalogo.Api.Contracts;

/// <summary>Respuesta de GET /metricas/prestamos-por-libro.</summary>
public sealed record PrestamosPorLibroResponse(int LibroId, int Prestamos);

/// <summary>Respuesta de GET /metricas/cuarentena.</summary>
public sealed record MensajeEnCuarentenaResponse(DateTime FechaUtc, string Motivo, string Contenido);
