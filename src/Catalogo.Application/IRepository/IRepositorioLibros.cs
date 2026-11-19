using Catalogo.Domain.Entities;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Application.IRepository;

/// <summary>
/// Puerto de persistencia que necesita la capa de aplicación.
/// La implementación concreta (en memoria hoy, base de datos después) vive fuera de esta capa.
/// </summary>
public interface IRepositorioLibros
{
    IReadOnlyList<Libro> Listar();
    Libro? ObtenerPorId(int id);
    bool ExisteCodigo(CodigoLibro codigo);
    void Agregar(Libro libro);
    void Actualizar(Libro libro);
}
