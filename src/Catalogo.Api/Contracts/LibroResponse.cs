namespace Catalogo.Api.Contracts;

/// <summary>
/// Respuesta HTTP de un libro. Solo expone los datos públicos: nunca la entidad Libro
/// ni datos internos del dominio.
/// </summary>
public sealed record LibroResponse(int Id, string Codigo, string Titulo, bool Activo);
