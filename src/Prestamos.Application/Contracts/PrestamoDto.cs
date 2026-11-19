namespace Prestamos.Application.Contracts;

/// <summary>
/// Salida de los casos de uso: nunca se expone la entidad Prestamo hacia afuera.
/// </summary>
public sealed record PrestamoDto(
    int Id,
    int LibroId,
    string Lector,
    DateOnly FechaInicio,
    DateOnly FechaLimite,
    DateOnly? FechaDevolucion,
    string Estado);
