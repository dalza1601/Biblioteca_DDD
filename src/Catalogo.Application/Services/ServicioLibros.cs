using Catalogo.Application.Contracts;
using Catalogo.Application.Exceptions;
using Catalogo.Application.IRepository;
using Catalogo.Domain.Entities;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Application.Services;

/// <summary>
/// Casos de uso del catálogo. Orquesta: obtiene y guarda a través del repositorio
/// y delega las reglas de negocio en la entidad Libro y el Value Object CodigoLibro.
/// </summary>
public sealed class ServicioLibros
{
    private readonly IRepositorioLibros _repositorio;

    public ServicioLibros(IRepositorioLibros repositorio)
    {
        _repositorio = repositorio;
    }

    public IReadOnlyList<LibroDto> Listar() =>
        _repositorio.Listar().Select(ADto).ToList();

    public LibroDto ObtenerPorId(int id) =>
        ADto(ObtenerExistente(id));

    public LibroDto Registrar(RegistrarLibroComando comando)
    {
        var codigo = CodigoLibro.Crear(comando.Codigo);

        // La unicidad necesita consultar todos los libros, por eso se verifica aquí y no en la entidad.
        // Se valida antes de crear el libro para no consumir un Id en un registro rechazado.
        if (_repositorio.ExisteCodigo(codigo))
            throw new ConflictoException($"Ya existe un libro con el código {codigo}.");

        var libro = Libro.Crear(codigo, comando.Titulo);
        _repositorio.Agregar(libro);

        return ADto(libro);
    }

    public LibroDto Desactivar(int id)
    {
        var libro = ObtenerExistente(id);

        libro.Desactivar();
        _repositorio.Actualizar(libro);

        return ADto(libro);
    }

    private Libro ObtenerExistente(int id) =>
        _repositorio.ObtenerPorId(id)
        ?? throw new RecursoNoEncontradoException($"No existe un libro con id {id}.");

    private static LibroDto ADto(Libro libro) =>
        new(libro.Id, libro.Codigo.Valor, libro.Titulo, libro.Activo);
}
