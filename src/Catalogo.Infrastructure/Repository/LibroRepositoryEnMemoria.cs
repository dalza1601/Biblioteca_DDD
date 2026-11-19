using Catalogo.Application.IRepository;
using Catalogo.Domain.Entities;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Infrastructure.Repository;

/// <summary>
/// Adaptador de persistencia en memoria para el puerto IRepositorioLibros.
/// TEMPORAL: se reemplazará por una implementación con base de datos.
/// </summary>
public sealed class LibroRepositoryEnMemoria : IRepositorioLibros
{
    private readonly List<Libro> _libros = new();
    private readonly Lock _lock = new();

    public IReadOnlyList<Libro> Listar()
    {
        lock (_lock)
            return _libros.ToList();
    }

    public Libro? ObtenerPorId(int id)
    {
        lock (_lock)
            return _libros.FirstOrDefault(libro => libro.Id == id);
    }

    public bool ExisteCodigo(CodigoLibro codigo)
    {
        lock (_lock)
            return _libros.Any(libro => libro.Codigo == codigo);
    }

    public void Agregar(Libro libro)
    {
        lock (_lock)
            _libros.Add(libro);
    }

    public void Actualizar(Libro libro)
    {
        // En memoria la lista guarda la misma instancia, así que los cambios ya están aplicados.
        // Con una base de datos aquí se persistirían.
    }
}
