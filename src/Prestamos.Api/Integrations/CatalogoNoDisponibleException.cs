namespace Prestamos.Api.Integrations;

/// <summary>
/// Catálogo no se pudo usar: no acepta conexiones o respondió con un error distinto de 404.
/// </summary>
public sealed class CatalogoNoDisponibleException : Exception
{
    public CatalogoNoDisponibleException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
}
