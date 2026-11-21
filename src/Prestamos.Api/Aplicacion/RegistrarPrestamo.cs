using Prestamos.Api.Integrations;
using Prestamos.Application.Contracts;
using Prestamos.Application.IRepository;
using Prestamos.Application.Mappers;
using Prestamos.Domain.Entities;
using Prestamos.Domain.Exceptions;
using Prestamos.Domain.ValueObjects;

namespace Prestamos.Api.Aplicacion;

/// <summary>
/// Caso de uso "registrar préstamo":
///   1. Validaciones baratas, sin red (CodigoLector primero y campos obligatorios).
///   2. Consulta el libro a Catálogo y decide:
///        200 activo   → continúa (Creado)
///        200 inactivo → LibroNoDisponible
///        null (404)   → LibroNoExiste
///        CatalogoNoDisponibleException → CatalogoNoDisponible
///        CatalogoTimeoutException      → CatalogoTimeout
///   3. Recién entonces aplica las reglas del dominio con Prestamo.Registrar() y guarda.
/// </summary>
public sealed class RegistrarPrestamo
{
    private readonly ILibroConsultaClient _catalogo;
    private readonly IRepositorioPrestamos _repositorio;

    public RegistrarPrestamo(ILibroConsultaClient catalogo, IRepositorioPrestamos repositorio)
    {
        _catalogo = catalogo;
        _repositorio = repositorio;
    }

    public async Task<ResultadoRegistrarPrestamo> EjecutarAsync(RegistrarPrestamoComando comando, CancellationToken ct)
    {
        // 1. Validaciones baratas: si fallan, no vale la pena llamar a Catálogo (→ 400 en el middleware).
        var lector = CodigoLector.Crear(comando.Lector);

        if (comando.LibroId is not { } libroId || libroId <= 0)
            throw new ValidacionDominioException("El identificador del libro debe ser mayor que cero.");

        var fechaInicio = comando.FechaInicio
            ?? throw new ValidacionDominioException("La fecha de inicio es obligatoria.");
        var fechaLimite = comando.FechaLimite
            ?? throw new ValidacionDominioException("La fecha límite es obligatoria.");

        // 2. Consulta a Catálogo y decisión según la tabla.
        LibroConsultaDto? libro;
        try
        {
            libro = await _catalogo.ObtenerLibroAsync(libroId, ct);
        }
        catch (CatalogoTimeoutException)
        {
            return new ResultadoRegistrarPrestamo.CatalogoTimeout();
        }
        catch (CatalogoNoDisponibleException)
        {
            return new ResultadoRegistrarPrestamo.CatalogoNoDisponible();
        }

        if (libro is null)
            return new ResultadoRegistrarPrestamo.LibroNoExiste(libroId);

        if (!libro.Activo)
            return new ResultadoRegistrarPrestamo.LibroNoDisponible(libroId);

        // 3. Reglas del dominio (p. ej. fecha límite posterior al inicio) y persistencia.
        var prestamo = Prestamo.Registrar(libroId, lector, fechaInicio, fechaLimite);
        _repositorio.Agregar(prestamo);

        return new ResultadoRegistrarPrestamo.Creado(prestamo.ADto());
    }
}
