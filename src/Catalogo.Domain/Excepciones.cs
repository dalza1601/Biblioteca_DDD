namespace Catalogo.Domain;

/// <summary>
/// Se lanza cuando un dato recibido no es válido (vacío, mal formado, etc.).
/// </summary>
public sealed class ValidacionDominioException : Exception
{
    public ValidacionDominioException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Se lanza cuando una operación viola una regla de negocio según el estado actual del objeto.
/// </summary>
public sealed class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}
