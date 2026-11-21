using Prestamos.Application.Contracts;

namespace Prestamos.Application.IClients;

/// <summary>
/// <para><b>Integración:</b> Catálogo (HTTP → Catalogo.Api, GET /libros/{id}) · <b>Tipo:</b> Puerto de consulta (interfaz)</para>
/// Lo que la aplicación necesita saber de Catálogo. La implementación HTTP vive en Prestamos.Infrastructure.
/// </summary>
public interface ILibroCatalogoClient
{
    /// <summary>
    /// Devuelve el libro, o null solo si Catálogo responde 404 (el libro no existe).
    /// Lanza CatalogoNoDisponibleException (sin conexión, 5xx u otro código) o
    /// CatalogoTimeoutException (venció el timeout interno). Si el cliente cancela, propaga la cancelación.
    /// </summary>
    Task<LibroConsultaDto?> ObtenerLibroAsync(int id, CancellationToken ct);
}
