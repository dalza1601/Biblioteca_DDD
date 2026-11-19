namespace Prestamos.Domain;

/// <summary>
/// Se lanza cuando un dato recibido no es válido (vacío, mal formado, fuera de rango, etc.).
/// Duplicada a propósito: cada bounded context es independiente.
/// </summary>
public sealed class ValidacionDominioException : Exception
{
    public ValidacionDominioException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Se lanza cuando una operación viola una regla de negocio según el estado actual del objeto.
/// Duplicada a propósito: cada bounded context es independiente.
/// </summary>
public sealed class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}
