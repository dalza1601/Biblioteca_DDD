namespace Catalogo.Application.Contracts;

/// <summary>
/// Entrada del caso de uso "registrar libro".
/// </summary>
public sealed record RegistrarLibroComando(string? Codigo, string? Titulo);
