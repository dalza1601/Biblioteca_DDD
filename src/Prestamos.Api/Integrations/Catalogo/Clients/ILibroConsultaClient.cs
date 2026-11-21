using Prestamos.Api.Integrations.Catalogo.Contracts;

namespace Prestamos.Api.Integrations.Catalogo.Clients;

/// <summary>
/// <para><b>Integración:</b> Catálogo (HTTP → Catalogo.Api, GET /libros/{id}) · <b>Tipo:</b> Interfaz del cliente HTTP (puerto)</para>
/// Consulta a la API de Catálogo los datos de un libro.
/// </summary>
public interface ILibroConsultaClient
{
    /// <summary>
    /// Devuelve el libro, o null solo si Catálogo responde 404 (el libro no existe).
    /// </summary>
    Task<LibroConsultaDto?> ObtenerLibroAsync(int id, CancellationToken ct);
}
