namespace Prestamos.Api.Integrations.Catalogo.Contracts;

/// <summary>
/// <para><b>Integración:</b> Catálogo (HTTP → Catalogo.Api, GET /libros/{id}) · <b>Tipo:</b> DTO de lectura (contrato de respuesta)</para>
/// Copia local (solo lectura) de lo que Catálogo publica sobre un libro en GET /libros/{id}.
/// Es un contrato de integración propio de Préstamos: no referencia Catalogo.Domain.
/// </summary>
public sealed record LibroConsultaDto(int Id, string Codigo, string Titulo, bool Activo);
