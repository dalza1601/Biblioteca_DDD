using Prestamos.Application.Contracts;
using Prestamos.Application.Exceptions;
using Prestamos.Application.IRepository;
using Prestamos.Domain.Entities;
using Prestamos.Domain.Exceptions;
using Prestamos.Domain.ValueObjects;

namespace Prestamos.Application.Services;

/// <summary>
/// Casos de uso de préstamos. Orquesta: obtiene y guarda a través del repositorio
/// y delega las reglas de negocio en el agregado Prestamo y el Value Object CodigoLector.
/// No consulta a Catálogo: solo guarda el LibroId recibido.
/// </summary>
public sealed class ServicioPrestamos
{
    private readonly IRepositorioPrestamos _repositorio;

    public ServicioPrestamos(IRepositorioPrestamos repositorio)
    {
        _repositorio = repositorio;
    }

    public IReadOnlyList<PrestamoDto> Listar() =>
        _repositorio.Listar().Select(ADto).ToList();

    public PrestamoDto ObtenerPorId(int id) =>
        ADto(ObtenerExistente(id));

    public PrestamoDto Registrar(RegistrarPrestamoComando comando)
    {
        var fechaInicio = comando.FechaInicio
            ?? throw new ValidacionDominioException("La fecha de inicio es obligatoria.");
        var fechaLimite = comando.FechaLimite
            ?? throw new ValidacionDominioException("La fecha límite es obligatoria.");

        var prestamo = Prestamo.Registrar(
            comando.LibroId ?? 0, // el dominio rechaza 0: "LibroId no puede ser nulo o 0"
            CodigoLector.Crear(comando.Lector),
            fechaInicio,
            fechaLimite);

        _repositorio.Agregar(prestamo);

        return ADto(prestamo);
    }

    public PrestamoDto Devolver(int id, DevolverPrestamoComando comando)
    {
        var prestamo = ObtenerExistente(id);

        var fechaDevolucion = comando.FechaDevolucion
            ?? throw new ValidacionDominioException("La fecha de devolución es obligatoria.");

        prestamo.Devolver(fechaDevolucion);
        _repositorio.Actualizar(prestamo);

        return ADto(prestamo);
    }

    private Prestamo ObtenerExistente(int id) =>
        _repositorio.ObtenerPorId(id)
        ?? throw new RecursoNoEncontradoException($"No existe un préstamo con id {id}.");

    private static PrestamoDto ADto(Prestamo prestamo) =>
        new(
            prestamo.Id,
            prestamo.LibroId,
            prestamo.Lector.Valor,
            prestamo.FechaInicio,
            prestamo.FechaLimite,
            prestamo.FechaDevolucion,
            prestamo.Estado.ToString());
}
