using Catalogo.Application.IRepository;
using Catalogo.Domain.Entities;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Infrastructure.Repository;

/// <summary>
/// Adaptador de persistencia en memoria para el puerto IRepositorioLibros.
/// TEMPORAL: se reemplazará por una implementación con base de datos.
/// En memoria no hay E/S que cancelar, pero se respeta el token antes de cada operación.
/// </summary>
public sealed class LibroRepositoryEnMemoria : IRepositorioLibros
{
    private readonly List<Libro> _libros = new();
    private readonly Lock _lock = new();

    public Task<IReadOnlyList<Libro>> ListarAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
            return Task.FromResult<IReadOnlyList<Libro>>(_libros.ToList());
    }

    public Task<Libro?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
            return Task.FromResult(_libros.FirstOrDefault(libro => libro.Id == id));
    }

    public Task<bool> ExisteCodigoAsync(CodigoLibro codigo, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
            return Task.FromResult(_libros.Any(libro => libro.Codigo == codigo));
    }

    public Task AgregarAsync(Libro libro, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
            _libros.Add(libro);

        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Libro libro, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // En memoria la lista guarda la misma instancia, así que los cambios ya están aplicados.
        // Con una base de datos aquí se persistirían.
        return Task.CompletedTask;
    }
}
