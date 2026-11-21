namespace Prestamos.Api.Integrations;

/// <summary>
/// Consulta a la API de Catálogo los datos de un libro.
/// </summary>
public interface ILibroConsultaClient
{
    /// <summary>
    /// Devuelve el libro, o null solo si Catálogo responde 404 (el libro no existe).
    /// </summary>
    Task<LibroConsultaDto?> ObtenerLibroAsync(int id, CancellationToken ct);
}
