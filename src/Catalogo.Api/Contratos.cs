namespace Catalogo.Api;

// Requests
public sealed record RegistrarLibroRequest(string? Codigo, string? Titulo);

// Responses
public sealed record LibroResponse(int Id, string Codigo, string Titulo, bool Activo);

public sealed record ErrorResponse(string Error);
