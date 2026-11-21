using Catalogo.Api.Mensajeria.Contracts;
using Catalogo.Api.Mensajeria.Options;
using Catalogo.Api.Mensajeria.Processing;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Catalogo.Api.Mensajeria.Consumers;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ, consumo de prestamo.registrado) · <b>Tipo:</b> Consumidor (BackgroundService)</para>
/// Consume la cola con la API asíncrona de RabbitMQ.Client 7.x (AsyncEventingBasicConsumer, sin .Result ni .Wait()).
///   - Declara (idempotente) el exchange topic durable, la cola durable y el binding prestamo.registrado.
///   - autoAck = false: confirma cada mensaje después de procesarlo.
///     Procesado, duplicado o inválido (ya quedó en cuarentena) → ack.
///     Excepción inesperada → nack; se reencola una sola vez (si ya fue reentregado, se descarta).
///   - Opción de prueba RabbitMq:CaidasAntesDeAck = N: las primeras N entregas simulan una caída
///     después de procesar y antes del ACK (nack con requeue) para comprobar la idempotencia.
///   - Si RabbitMQ no está disponible al arrancar, reintenta cada 5 s sin detener la API.
///     Una vez conectado, la recuperación automática del cliente restablece conexión y consumidor.
/// </summary>
public sealed class PrestamoRegistradoConsumer : BackgroundService
{
    private static readonly TimeSpan EsperaReconexion = TimeSpan.FromSeconds(5);

    private readonly RabbitMqOptions _opciones;
    private readonly ProcesadorPrestamoRegistrado _procesador;
    private readonly ILogger<PrestamoRegistradoConsumer> _logger;

    private IConnection? _conexion;
    private IChannel? _canal;
    private int _caidasSimuladas;

    public PrestamoRegistradoConsumer(
        IOptions<RabbitMqOptions> opciones,
        ProcesadorPrestamoRegistrado procesador,
        ILogger<PrestamoRegistradoConsumer> logger)
    {
        _opciones = opciones.Value;
        _procesador = procesador;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConectarYConsumirAsync(stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken); // consume hasta que la API se detenga
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo conectar a RabbitMQ {Host}:{Puerto}; nuevo intento en {Segundos} s.",
                    _opciones.HostName, _opciones.Port, EsperaReconexion.TotalSeconds);
                await CerrarAsync();

                try { await Task.Delay(EsperaReconexion, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task ConectarYConsumirAsync(CancellationToken ct)
    {
        var fabrica = new ConnectionFactory
        {
            HostName = _opciones.HostName,
            Port = _opciones.Port,
            ClientProvidedName = "catalogo-api",
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };
        if (!string.IsNullOrEmpty(_opciones.UserName)) fabrica.UserName = _opciones.UserName;
        if (!string.IsNullOrEmpty(_opciones.Password)) fabrica.Password = _opciones.Password;

        _conexion = await fabrica.CreateConnectionAsync(ct);
        _canal = await _conexion.CreateChannelAsync(cancellationToken: ct);

        await _canal.ExchangeDeclareAsync(_opciones.Exchange, ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: ct);
        await _canal.QueueDeclareAsync(_opciones.Cola,
            durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await _canal.QueueBindAsync(_opciones.Cola, _opciones.Exchange, PrestamoRegistradoMensaje.RoutingKey,
            cancellationToken: ct);
        await _canal.BasicQosAsync(prefetchSize: 0, prefetchCount: _opciones.Prefetch, global: false, ct);

        var consumidor = new AsyncEventingBasicConsumer(_canal);
        consumidor.ReceivedAsync += AlRecibirAsync;
        await _canal.BasicConsumeAsync(_opciones.Cola, autoAck: false, consumer: consumidor, cancellationToken: ct);

        _logger.LogInformation("Consumiendo {Cola} (exchange {Exchange}, binding {RoutingKey}).",
            _opciones.Cola, _opciones.Exchange, PrestamoRegistradoMensaje.RoutingKey);
    }

    private async Task AlRecibirAsync(object sender, BasicDeliverEventArgs entrega)
    {
        var canal = ((AsyncEventingBasicConsumer)sender).Channel;

        try
        {
            // Body solo es válido durante este manejador: se copia antes de procesar.
            var resultado = _procesador.Procesar(entrega.Body.ToArray(), entrega.Redelivered);

            if (DebeSimularCaida(out var caida))
            {
                // Simulación: el efecto ya se aplicó, pero el ACK nunca llega. Se devuelve el mensaje
                // a la cola (como haría el broker si el consumidor muriera) y RabbitMQ lo reentrega
                // marcado como Redelivered. La idempotencia del procesador evita contarlo dos veces.
                _logger.LogWarning(
                    "SIMULACIÓN {Caida}/{Total}: caída después de procesar ({Resultado}) y antes del ACK; " +
                    "el mensaje {DeliveryTag} se reentregará.",
                    caida, _opciones.CaidasAntesDeAck, resultado, entrega.DeliveryTag);
                await canal.BasicNackAsync(entrega.DeliveryTag, multiple: false, requeue: true);
                return;
            }

            await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            var reencolar = !entrega.Redelivered;
            _logger.LogError(ex, "Error inesperado procesando el mensaje {DeliveryTag}; reencolar = {Reencolar}.",
                entrega.DeliveryTag, reencolar);
            await canal.BasicNackAsync(entrega.DeliveryTag, multiple: false, requeue: reencolar);
        }
    }

    /// <summary>Consume una de las caídas simuladas configuradas en CaidasAntesDeAck (si quedan).</summary>
    private bool DebeSimularCaida(out int caida)
    {
        caida = 0;
        if (_opciones.CaidasAntesDeAck <= 0)
            return false;

        caida = Interlocked.Increment(ref _caidasSimuladas);
        return caida <= _opciones.CaidasAntesDeAck;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await CerrarAsync();
    }

    private async Task CerrarAsync()
    {
        if (_canal is not null)
        {
            try { await _canal.DisposeAsync(); } catch { /* ya estaba cerrado */ }
            _canal = null;
        }

        if (_conexion is not null)
        {
            try { await _conexion.DisposeAsync(); } catch { /* ya estaba cerrada */ }
            _conexion = null;
        }
    }
}
