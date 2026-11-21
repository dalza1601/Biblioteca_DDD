namespace Prestamos.Application.Exceptions;

/// <summary>
/// <para><b>Integración:</b> Catálogo (HTTP → Catalogo.Api) · <b>Tipo:</b> Excepción de integración (Catálogo no disponible → 503)</para>
/// Catálogo no se pudo usar: no acepta conexiones o respondió con un error distinto de 404.
/// </summary>
public sealed class CatalogoNoDisponibleException : Exception
{
    public CatalogoNoDisponibleException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
}
