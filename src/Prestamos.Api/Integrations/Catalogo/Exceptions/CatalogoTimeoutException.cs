namespace Prestamos.Api.Integrations.Catalogo.Exceptions;

/// <summary>
/// <para><b>Integración:</b> Catálogo (HTTP → Catalogo.Api, GET /libros/{id}) · <b>Tipo:</b> Excepción de integración (timeout → 504)</para>
/// Catálogo no respondió dentro del timeout configurado (CatalogoApi:TimeoutSeconds).
/// </summary>
public sealed class CatalogoTimeoutException : Exception
{
    public CatalogoTimeoutException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
}
