using Catalogo.Domain;

namespace Catalogo.Api;

/// <summary>
/// Almacenamiento en memoria (sin base de datos todavía). Usa el Id que ya trae la entidad.
/// </summary>
public sealed class RepositorioLibros
{
    private readonly Dictionary<int, Libro> _libros = new();
    private readonly Lock _lock = new();

    public IReadOnlyList<Libro> Listar()
    {
        lock (_lock)
            return _libros.Values.OrderBy(libro => libro.Id).ToList();
    }

    public Libro? ObtenerPorId(int id)
    {
        lock (_lock)
            return _libros.GetValueOrDefault(id);
    }

    public bool ExisteCodigo(CodigoLibro codigo)
    {
        lock (_lock)
            return _libros.Values.Any(libro => libro.Codigo == codigo);
    }

    public void Agregar(Libro libro)
    {
        lock (_lock)
        {
            if (_libros.Values.Any(existente => existente.Codigo == libro.Codigo))
                throw new ReglaNegocioException($"Ya existe un libro con el código {libro.Codigo}.");

            _libros[libro.Id] = libro;
        }
    }
}
