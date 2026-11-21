using Prestamos.Application.Contracts;

namespace Prestamos.Api.Aplicacion;

/// <summary>
/// Resultado del caso de uso RegistrarPrestamo. Cada tipo es un desenlace esperado del negocio
/// o de la integración con Catálogo (no excepciones): PrestamoHttp.Traducir lo convierte a HTTP.
/// </summary>
public abstract record ResultadoRegistrarPrestamo
{
    private ResultadoRegistrarPrestamo() { }

    /// <summary>Préstamo registrado.</summary>
    public sealed record Creado(PrestamoDto Prestamo) : ResultadoRegistrarPrestamo;

    /// <summary>El libro existe en Catálogo pero está inactivo.</summary>
    public sealed record LibroNoDisponible(int LibroId) : ResultadoRegistrarPrestamo;

    /// <summary>Catálogo respondió 404: el libro no existe.</summary>
    public sealed record LibroNoExiste(int LibroId) : ResultadoRegistrarPrestamo;

    /// <summary>No se pudo conectar con Catálogo o respondió con error.</summary>
    public sealed record CatalogoNoDisponible : ResultadoRegistrarPrestamo;

    /// <summary>Catálogo no respondió dentro del timeout.</summary>
    public sealed record CatalogoTimeout : ResultadoRegistrarPrestamo;
}
