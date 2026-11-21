using Prestamos.Application.Contracts;
using Prestamos.Domain.Entities;

namespace Prestamos.Application.Mappers;

/// <summary>
/// Convierte el agregado Prestamo en su DTO de salida. Único punto de mapeo,
/// usado tanto por ServicioPrestamos como por el caso de uso RegistrarPrestamo.
/// </summary>
public static class PrestamoMapper
{
    public static PrestamoDto ADto(this Prestamo prestamo) =>
        new(
            prestamo.Id,
            prestamo.LibroId,
            prestamo.Lector.Valor,
            prestamo.FechaInicio,
            prestamo.FechaLimite,
            prestamo.FechaDevolucion,
            prestamo.Estado.ToString());
}
