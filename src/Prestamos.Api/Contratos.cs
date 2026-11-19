namespace Prestamos.Api;

// Requests
public sealed record RegistrarPrestamoRequest(int? LibroId, string? Lector, DateOnly? FechaInicio, DateOnly? FechaLimite);

public sealed record DevolverPrestamoRequest(DateOnly? FechaDevolucion);

// Responses
public sealed record PrestamoResponse(
    int Id,
    int LibroId,
    string Lector,
    DateOnly FechaInicio,
    DateOnly FechaLimite,
    DateOnly? FechaDevolucion,
    string Estado);

public sealed record ErrorResponse(string Error);
