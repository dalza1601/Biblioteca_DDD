namespace Prestamos.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una operación viola una regla de negocio según el estado actual del objeto.
/// </summary>
public sealed class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}
