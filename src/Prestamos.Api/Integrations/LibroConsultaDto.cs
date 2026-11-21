namespace Prestamos.Api.Integrations;

/// <summary>
/// Copia local (solo lectura) de lo que Catálogo publica sobre un libro en GET /libros/{id}.
/// Es un contrato de integración propio de Préstamos: no referencia Catalogo.Domain.
/// </summary>
public sealed record LibroConsultaDto(int Id, string Codigo, string Titulo, bool Activo);
