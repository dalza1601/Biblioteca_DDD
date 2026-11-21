namespace Prestamos.Application.Contracts;

/// <summary>
/// <para><b>Integración:</b> Catálogo (HTTP → Catalogo.Api, GET /libros/{id}) · <b>Tipo:</b> DTO de integración (lectura)</para>
/// Copia local (solo lectura) de lo que Catálogo publica sobre un libro.
/// Es un contrato propio de Préstamos: no referencia Catalogo.Domain.
/// </summary>
public sealed record LibroConsultaDto(int Id, string Codigo, string Titulo, bool Activo);
