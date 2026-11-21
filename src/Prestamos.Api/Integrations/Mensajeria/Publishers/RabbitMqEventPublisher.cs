using System.Text.Json;
using Microsoft.Extensions.Options;
using Prestamos.Api.Integrations.Mensajeria.Events;
using Prestamos.Api.Integrations.Mensajeria.Options;
using RabbitMQ.Client;

namespace Prestamos.Api.Integrations.Mensajeria.Publishers;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ) · <b>Tipo:</b> Publicador de eventos (adaptador, Singleton)</para>
/// Implementación con la API asíncrona de RabbitMQ.Client 7.x (sin .Result ni .Wait()).
///   - Una conexión y un canal compartidos, creados de forma perezosa en la primera publicación.
///   - Acceso serializado con SemaphoreSlim: un IChannel no admite publicaciones concurrentes.
///   - Topología declarada al abrir el canal (idempotente): exchange topic durable, cola durable y binding.
///   - Mensajes persistentes y con confirmación del broker (publisher confirms): si el broker no
///     confirma, PublicarAsync lanza excepción.
///   - Si algo falla, cierra conexión y canal; la siguiente publicación los recrea.
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _opciones;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    private IConnection? _conexion;
    private IChannel? _canal;

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> opciones, ILogger<RabbitMqEventPublisher> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task PublicarAsync<T>(string routingKey, T evento, CancellationToken ct = default)
    {
        var cuerpo = JsonSerializer.SerializeToUtf8Bytes(evento, OpcionesJson);
        var propiedades = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = typeof(T).Name,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _semaforo.WaitAsync(ct);
        try
        {
            var canal = await ObtenerCanalAsync(ct);

            // Con publisher confirms, esta llamada espera la confirmación del broker.
            await canal.BasicPublishAsync(_opciones.Exchange, routingKey, mandatory: true,
                basicProperties: propiedades, body: cuerpo, cancellationToken: ct);
        }
        catch
        {
            // Conexión o canal en estado dudoso: se descartan y la próxima publicación los recrea.
            await CerrarAsync();
            throw;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    // Se llama siempre dentro del semáforo.
    private async Task<IChannel> ObtenerCanalAsync(CancellationToken ct)
    {
        if (_conexion is { IsOpen: true } && _canal is { IsOpen: true })
            return _canal;

        await CerrarAsync();

        var fabrica = new ConnectionFactory
        {
            HostName = _opciones.HostName,
            Port = _opciones.Port,
            ClientProvidedName = "prestamos-api"
        };
        if (!string.IsNullOrEmpty(_opciones.UserName)) fabrica.UserName = _opciones.UserName;
        if (!string.IsNullOrEmpty(_opciones.Password)) fabrica.Password = _opciones.Password;

        _conexion = await fabrica.CreateConnectionAsync(ct);
        _canal = await _conexion.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);

        await _canal.ExchangeDeclareAsync(_opciones.Exchange, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: ct);
        await _canal.QueueDeclareAsync(_opciones.Cola,
            durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await _canal.QueueBindAsync(_opciones.Cola, _opciones.Exchange, PrestamoRegistrado.RoutingKey,
            cancellationToken: ct);

        _logger.LogInformation("Conectado a RabbitMQ {Host}:{Puerto}; exchange {Exchange} → cola {Cola} ({RoutingKey}).",
            _opciones.HostName, _opciones.Port, _opciones.Exchange, _opciones.Cola, PrestamoRegistrado.RoutingKey);

        return _canal;
    }

    private async Task CerrarAsync()
    {
        if (_canal is not null)
        {
            try { await _canal.DisposeAsync(); } catch { /* ya estaba roto */ }
            _canal = null;
        }

        if (_conexion is not null)
        {
            try { await _conexion.DisposeAsync(); } catch { /* ya estaba roto */ }
            _conexion = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _semaforo.WaitAsync();
        try { await CerrarAsync(); }
        finally { _semaforo.Release(); }

        _semaforo.Dispose();
    }
}
