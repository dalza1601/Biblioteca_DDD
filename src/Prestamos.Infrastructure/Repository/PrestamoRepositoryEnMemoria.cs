using Prestamos.Application.IRepository;
using Prestamos.Domain.Entities;

namespace Prestamos.Infrastructure.Repository;

/// <summary>
/// Adaptador de persistencia en memoria para el puerto IRepositorioPrestamos.
/// TEMPORAL: se reemplazará por una implementación con base de datos.
/// </summary>
public sealed class PrestamoRepositoryEnMemoria : IRepositorioPrestamos
{
    private readonly List<Prestamo> _prestamos = new();
    private readonly Lock _lock = new();

    public IReadOnlyList<Prestamo> Listar()
    {
        lock (_lock)
            return _prestamos.ToList();
    }

    public Prestamo? ObtenerPorId(int id)
    {
        lock (_lock)
            return _prestamos.FirstOrDefault(prestamo => prestamo.Id == id);
    }

    public void Agregar(Prestamo prestamo)
    {
        lock (_lock)
            _prestamos.Add(prestamo);
    }

    public void Actualizar(Prestamo prestamo)
    {
        // En memoria la lista guarda la misma instancia, así que los cambios ya están aplicados.
        // Con una base de datos aquí se persistirían.
    }
}
