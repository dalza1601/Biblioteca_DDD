namespace Prestamos.Application.Exceptions;

/// <summary>
/// Se lanza cuando la operación choca con el estado de otros agregados (por ejemplo, un código duplicado).
/// </summary>
public sealed class ConflictoException : Exception
{
    public ConflictoException(string mensaje) : base(mensaje) { }
}
