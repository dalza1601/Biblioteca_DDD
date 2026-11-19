namespace Prestamos.Application.Contracts;

/// <summary>
/// Entrada del caso de uso "registrar préstamo". Los campos son opcionales para validar
/// su presencia en el servicio y responder con un mensaje claro.
/// </summary>
public sealed record RegistrarPrestamoComando(int? LibroId, string? Lector, DateOnly? FechaInicio, DateOnly? FechaLimite);
