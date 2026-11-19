using Prestamos.Domain.Enums;
using Prestamos.Domain.Exceptions;
using Prestamos.Domain.ValueObjects;

namespace Prestamos.Domain.Entities;

/// <summary>
/// Aggregate Root del contexto Préstamos. Del libro solo conserva su identificador (LibroId);
/// no conoce la clase Libro, que pertenece al contexto Catálogo.
/// </summary>
public sealed class Prestamo
{
    // TEMPORAL: mientras no exista persistencia, el Id se asigna con un contador en memoria (1, 2, 3...).
    // Al implementar la base de datos, el Id lo generará la persistencia y este contador debe eliminarse.
    private static int _ultimoId;

    public int Id { get; private set; }
    public int LibroId { get; private set; }
    public CodigoLector Lector { get; private set; }
    public DateOnly FechaInicio { get; private set; }
    public DateOnly FechaLimite { get; private set; }
    public DateOnly? FechaDevolucion { get; private set; }
    public EstadoPrestamo Estado { get; private set; }

    private Prestamo(int libroId, CodigoLector lector, DateOnly fechaInicio, DateOnly fechaLimite)
    {
        Id = Interlocked.Increment(ref _ultimoId);
        LibroId = libroId;
        Lector = lector;
        FechaInicio = fechaInicio;
        FechaLimite = fechaLimite;
        Estado = EstadoPrestamo.Activo;
    }

    public static Prestamo Registrar(int libroId, CodigoLector lector, DateOnly fechaInicio, DateOnly fechaLimite)
    {
        if (libroId <= 0)
            throw new ValidacionDominioException("El identificador del libro debe ser mayor que cero.");

        if (lector is null)
            throw new ValidacionDominioException("El lector es obligatorio.");

        if (fechaLimite <= fechaInicio)
            throw new ValidacionDominioException("La fecha límite debe ser posterior a la fecha de inicio.");

        return new Prestamo(libroId, lector, fechaInicio, fechaLimite);
    }

    public void Devolver(DateOnly fecha)
    {
        if (Estado == EstadoPrestamo.Devuelto)
            throw new ReglaNegocioException("El préstamo ya fue devuelto.");

        if (fecha < FechaInicio)
            throw new ValidacionDominioException("La fecha de devolución no puede ser anterior a la fecha de inicio.");

        FechaDevolucion = fecha;
        Estado = EstadoPrestamo.Devuelto;
    }

    /// <summary>
    /// Un préstamo está vencido solo si sigue Activo y la fecha actual es posterior a la FechaLimite.
    /// El mismo día del límite todavía no está vencido.
    /// </summary>
    public bool EstaVencido(DateOnly fechaActual) =>
        Estado == EstadoPrestamo.Activo && fechaActual > FechaLimite;
}
