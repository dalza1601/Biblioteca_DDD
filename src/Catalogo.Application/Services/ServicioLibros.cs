using Catalogo.Application.Contracts;
using Catalogo.Application.Exceptions;
using Catalogo.Application.IRepository;
using Catalogo.Domain.Entities;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Application.Services;

/// <summary>
/// Casos de uso del catálogo. Orquesta: obtiene y guarda a través del repositorio
/// y delega las reglas de negocio en la entidad Libro y el Value Object CodigoLibro.
/// Cada caso de uso recibe el CancellationToken y lo propaga al repositorio.
/// </summary>
public sealed class ServicioLibros
{
    private readonly IRepositorioLibros _repositorio;

    public ServicioLibros(IRepositorioLibros repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<IReadOnlyList<LibroDto>> ListarAsync(CancellationToken cancellationToken)
    {
        var libros = await _repositorio.ListarAsync(cancellationToken);
        return libros.Select(ADto).ToList();
    }

    public async Task<LibroDto> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)
    {
        // Un libro desactivado también se devuelve (Activo = false): solo es 404 si no existe.
        var libro = await _repositorio.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNoEncontradoException("Libro no encontrado");

        return ADto(libro);
    }

    public async Task<LibroDto> RegistrarAsync(RegistrarLibroComando comando, CancellationToken cancellationToken)
    {
        var codigo = CodigoLibro.Crear(comando.Codigo);

        // La unicidad necesita consultar todos los libros, por eso se verifica aquí y no en la entidad.
        // Se valida antes de crear el libro para no consumir un Id en un registro rechazado.
        if (await _repositorio.ExisteCodigoAsync(codigo, cancellationToken))
            throw new ConflictoException($"Ya existe un libro con el código {codigo}.");

        var libro = Libro.Crear(codigo, comando.Titulo);
        await _repositorio.AgregarAsync(libro, cancellationToken);

        return ADto(libro);
    }

    public async Task<LibroDto> DesactivarAsync(int id, CancellationToken cancellationToken)
    {
        var libro = await ObtenerExistenteAsync(id, cancellationToken);

        libro.Desactivar();
        await _repositorio.ActualizarAsync(libro, cancellationToken);

        return ADto(libro);
    }

    private async Task<Libro> ObtenerExistenteAsync(int id, CancellationToken cancellationToken) =>
        await _repositorio.ObtenerPorIdAsync(id, cancellationToken)
        ?? throw new RecursoNoEncontradoException($"No existe un libro con id {id}.");

    private static LibroDto ADto(Libro libro) =>
        new(libro.Id, libro.Codigo.Valor, libro.Titulo, libro.Activo);
}
