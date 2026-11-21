using Prestamos.Application.Contracts;
using Prestamos.Application.Exceptions;
using Prestamos.Application.IRepository;
using Prestamos.Application.Mappers;
using Prestamos.Domain.Entities;
using Prestamos.Domain.Exceptions;

namespace Prestamos.Application.Services;

/// <summary>
/// Casos de uso de préstamos que no necesitan consultar a Catálogo: listar, obtener y devolver.
/// Orquesta a través del repositorio y delega las reglas de negocio en el agregado Prestamo.
/// El registro de préstamos (que sí consulta a Catálogo) vive en el caso de uso RegistrarPrestamo.
/// </summary>
public sealed class ServicioPrestamos
{
    private readonly IRepositorioPrestamos _repositorio;

    public ServicioPrestamos(IRepositorioPrestamos repositorio)
    {
        _repositorio = repositorio;
    }

    public IReadOnlyList<PrestamoDto> Listar() =>
        _repositorio.Listar().Select(prestamo => prestamo.ADto()).ToList();

    public PrestamoDto ObtenerPorId(int id) =>
        ObtenerExistente(id).ADto();

    public PrestamoDto Devolver(int id, DevolverPrestamoComando comando)
    {
        var prestamo = ObtenerExistente(id);

        var fechaDevolucion = comando.FechaDevolucion
            ?? throw new ValidacionDominioException("La fecha de devolución es obligatoria.");

        prestamo.Devolver(fechaDevolucion);
        _repositorio.Actualizar(prestamo);

        return prestamo.ADto();
    }

    private Prestamo ObtenerExistente(int id) =>
        _repositorio.ObtenerPorId(id)
        ?? throw new RecursoNoEncontradoException($"No existe un préstamo con id {id}.");
}
