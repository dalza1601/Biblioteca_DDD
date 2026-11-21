namespace Prestamos.Api.Integrations;

/// <summary>
/// Catálogo no respondió dentro del timeout configurado (CatalogoApi:TimeoutSeconds).
/// </summary>
public sealed class CatalogoTimeoutException : Exception
{
    public CatalogoTimeoutException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
}
