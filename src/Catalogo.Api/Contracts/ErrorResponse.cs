namespace Catalogo.Api.Contracts;

/// <summary>
/// Forma única de toda respuesta de error: {"error": "mensaje"}.
/// </summary>
public sealed record ErrorResponse(string Error);
