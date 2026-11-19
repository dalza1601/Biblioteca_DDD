using Prestamos.Domain;

namespace Prestamos.Api;

/// <summary>
/// Almacenamiento en memoria (sin base de datos todavía). Usa el Id que ya trae la entidad.
/// </summary>
public sealed class RepositorioPrestamos
{
    private readonly Dictionary<int, Prestamo> _prestamos = new();
    private readonly Lock _lock = new();

    public IReadOnlyList<Prestamo> Listar()
    {
        lock (_lock)
            return _prestamos.Values.OrderBy(prestamo => prestamo.Id).ToList();
    }

    public Prestamo? ObtenerPorId(int id)
    {
        lock (_lock)
            return _prestamos.GetValueOrDefault(id);
    }

    public void Agregar(Prestamo prestamo)
    {
        lock (_lock)
            _prestamos[prestamo.Id] = prestamo;
    }
}
