using Prestamos.Domain.Entities;

namespace Prestamos.Application.IRepository;

/// <summary>
/// Puerto de persistencia que necesita la capa de aplicación.
/// La implementación concreta (en memoria hoy, base de datos después) vive fuera de esta capa.
/// </summary>
public interface IRepositorioPrestamos
{
    IReadOnlyList<Prestamo> Listar();
    Prestamo? ObtenerPorId(int id);
    void Agregar(Prestamo prestamo);
    void Actualizar(Prestamo prestamo);
}
