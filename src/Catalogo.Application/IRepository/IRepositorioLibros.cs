using Catalogo.Domain.Entities;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Application.IRepository;

/// <summary>
/// Puerto de persistencia que necesita la capa de aplicación.
/// La implementación concreta (en memoria hoy, base de datos después) vive fuera de esta capa.
/// Todas las operaciones reciben el CancellationToken de la petición HTTP.
/// </summary>
public interface IRepositorioLibros
{
    Task<IReadOnlyList<Libro>> ListarAsync(CancellationToken cancellationToken);
    Task<Libro?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExisteCodigoAsync(CodigoLibro codigo, CancellationToken cancellationToken);
    Task AgregarAsync(Libro libro, CancellationToken cancellationToken);
    Task ActualizarAsync(Libro libro, CancellationToken cancellationToken);
}
