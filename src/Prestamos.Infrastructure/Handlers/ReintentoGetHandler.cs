using System.Net;
using Microsoft.Extensions.Logging;

namespace Prestamos.Infrastructure.Handlers;

/// <summary>
/// <para><b>Integración:</b> Catálogo (HTTP → Catalogo.Api) · <b>Tipo:</b> DelegatingHandler (política de reintentos GET)</para>
///   - Reintenta SOLO peticiones GET (idempotentes) cuando hubo HttpRequestException o respuesta 5xx.
///   - Máximo 3 intentos en total, con backoff exponencial entre intentos: 200 ms, 400 ms.
///   - Cualquier otro método (POST, PUT...) pasa directo, sin reintentos.
///   - No reintenta 4xx (p. ej. 404 es una respuesta válida) ni cancelaciones/timeouts.
/// El Timeout del HttpClient cubre todos los intentos juntos, no cada uno por separado.
/// Se registra solo si CatalogoApi:Reintentos > 1 (apagado por defecto).
/// </summary>
public sealed class ReintentoGetHandler : DelegatingHandler
{
    public const int MaximoIntentos = 3;
    private static readonly TimeSpan RetardoBase = TimeSpan.FromMilliseconds(200);

    private readonly int _maxIntentos;
    private readonly ILogger<ReintentoGetHandler> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _esperar;

    /// <param name="maxIntentos">Intentos totales (se limita a <see cref="MaximoIntentos"/>).</param>
    /// <param name="esperar">Espera entre intentos; por defecto Task.Delay. Las pruebas la sustituyen para no dormir.</param>
    public ReintentoGetHandler(
        int maxIntentos,
        ILogger<ReintentoGetHandler> logger,
        Func<TimeSpan, CancellationToken, Task>? esperar = null)
    {
        _maxIntentos = Math.Clamp(maxIntentos, 1, MaximoIntentos);
        _logger = logger;
        _esperar = esperar ?? Task.Delay;
    }

    /// <summary>Retardo antes del siguiente intento: 200 ms tras el 1.º fallo, 400 ms tras el 2.º.</summary>
    public static TimeSpan Retardo(int intentoFallido) =>
        RetardoBase * Math.Pow(2, intentoFallido - 1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get)
            return await base.SendAsync(request, cancellationToken);

        for (var intento = 1; ; intento++)
        {
            try
            {
                var respuesta = await base.SendAsync(request, cancellationToken);

                if (!EsErrorDeServidor(respuesta.StatusCode) || intento >= _maxIntentos)
                    return respuesta;

                _logger.LogWarning("GET {Ruta} respondió {StatusCode} (intento {Intento}/{Max}); se reintentará.",
                    request.RequestUri?.AbsolutePath, (int)respuesta.StatusCode, intento, _maxIntentos);
                respuesta.Dispose();
            }
            catch (HttpRequestException ex) when (intento < _maxIntentos)
            {
                _logger.LogWarning(ex, "GET {Ruta} falló al conectar (intento {Intento}/{Max}); se reintentará.",
                    request.RequestUri?.AbsolutePath, intento, _maxIntentos);
            }

            await _esperar(Retardo(intento), cancellationToken);
        }
    }

    private static bool EsErrorDeServidor(HttpStatusCode codigo) => (int)codigo >= 500;
}
