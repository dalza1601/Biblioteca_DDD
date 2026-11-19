namespace Prestamos.Domain.Exceptions;

/// <summary>
/// Se lanza cuando un dato recibido no es válido (vacío, mal formado, fuera de rango, etc.).
/// </summary>
public sealed class ValidacionDominioException : Exception
{
    public ValidacionDominioException(string mensaje) : base(mensaje) { }
}
