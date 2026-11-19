namespace Prestamos.Application.Contracts;

/// <summary>
/// Entrada del caso de uso "devolver préstamo".
/// </summary>
public sealed record DevolverPrestamoComando(DateOnly? FechaDevolucion);
