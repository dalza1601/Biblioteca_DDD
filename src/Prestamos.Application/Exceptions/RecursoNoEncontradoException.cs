namespace Prestamos.Application.Exceptions;

/// <summary>
/// Se lanza cuando un caso de uso no encuentra el recurso solicitado.
/// </summary>
public sealed class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string mensaje) : base(mensaje) { }
}
