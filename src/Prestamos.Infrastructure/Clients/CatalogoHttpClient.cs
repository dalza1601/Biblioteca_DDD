using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Prestamos.Application.Contracts;
using Prestamos.Application.Exceptions;
using Prestamos.Application.IClients;

namespace Prestamos.Infrastructure.Clients;

/// <summary>
/// <para><b>Integración:</b> Catálogo (HTTP → Catalogo.Api, GET /libros/{id}) · <b>Tipo:</b> Cliente HTTP tipado (adaptador de ILibroCatalogoClient)</para>
/// El HttpClient lo crea y configura IHttpClientFactory (BaseAddress y Timeout desde la sección CatalogoApi).
///
/// Traducción de resultados:
///   200            → LibroConsultaDto
///   404            → null (el libro no existe)
///   5xx u otro código inesperado       → CatalogoNoDisponibleException
///   HttpRequestException (sin conexión) → CatalogoNoDisponibleException
///   TaskCanceledException por timeout   → CatalogoTimeoutException
///   Cancelación pedida por el cliente   → se propaga tal cual (no es un 504)
///
/// No envía credenciales ni reenvía tokens del usuario. Los mensajes de las excepciones son
/// genéricos; el detalle técnico va solo al log.
/// </summary>
public sealed class CatalogoHttpClient : ILibroCatalogoClient
{
    private const string MensajeNoDisponible = "El servicio de Catálogo no está disponible.";
    private const string MensajeTimeout = "El servicio de Catálogo no respondió a tiempo.";

    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogoHttpClient> _logger;

    public CatalogoHttpClient(HttpClient httpClient, ILogger<CatalogoHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LibroConsultaDto?> ObtenerLibroAsync(int id, CancellationToken ct)
    {
        var ruta = $"libros/{id}";
        _logger.LogInformation("Consultando Catálogo: GET {Url}", new Uri(_httpClient.BaseAddress!, ruta));
        var reloj = Stopwatch.StartNew();

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await _httpClient.GetAsync(ruta, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo conectar con Catálogo al consultar el libro {LibroId}.", id);
            throw new CatalogoNoDisponibleException(MensajeNoDisponible, ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // El cliente de Préstamos no canceló: venció el Timeout configurado del HttpClient.
            _logger.LogWarning(ex, "Catálogo no respondió en {TimeoutSegundos} s al consultar el libro {LibroId}.",
                _httpClient.Timeout.TotalSeconds, id);
            throw new CatalogoTimeoutException(MensajeTimeout, ex);
        }
        // Si ct.IsCancellationRequested (el cliente canceló), la TaskCanceledException se propaga sin convertir.

        using (respuesta)
        {
            _logger.LogInformation("Catálogo respondió {StatusCode} para el libro {LibroId} en {Milisegundos} ms.",
                (int)respuesta.StatusCode, id, reloj.ElapsedMilliseconds);

            switch (respuesta.StatusCode)
            {
                case HttpStatusCode.OK:
                    return await respuesta.Content.ReadFromJsonAsync<LibroConsultaDto>(ct);

                case HttpStatusCode.NotFound:
                    return null;

                default:
                    // 5xx o cualquier código que no forma parte del contrato (3xx, 400, 401, 204...).
                    _logger.LogWarning("Catálogo respondió {StatusCode} al consultar el libro {LibroId}.",
                        (int)respuesta.StatusCode, id);
                    throw new CatalogoNoDisponibleException(MensajeNoDisponible);
            }
        }
    }
}
