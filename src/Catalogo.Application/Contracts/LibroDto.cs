namespace Catalogo.Application.Contracts;

/// <summary>
/// Salida de los casos de uso: nunca se expone la entidad Libro hacia afuera.
/// </summary>
public sealed record LibroDto(int Id, string Codigo, string Titulo, bool Activo);
