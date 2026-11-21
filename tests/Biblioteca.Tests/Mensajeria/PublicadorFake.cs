using Prestamos.Api.Integrations.Mensajeria.Publishers;

namespace Biblioteca.Tests.Mensajeria;

/// <summary>
/// Fake de IEventPublisher: no usa RabbitMQ.
///   - QueFunciona(): guarda cada evento publicado con su routing key.
///   - QueFalla(ex): lanza la excepción para simular un broker caído.
///   - QueSeCuelga(): no termina hasta que se cancela el token (para probar el timeout de 4 s).
/// </summary>
internal sealed class PublicadorFake : IEventPublisher
{
    private readonly Exception? _excepcion;
    private readonly bool _colgarse;

    private PublicadorFake(Exception? excepcion, bool colgarse)
    {
        _excepcion = excepcion;
        _colgarse = colgarse;
    }

    public List<(string RoutingKey, object Evento)> Publicados { get; } = new();
    public int Intentos { get; private set; }

    public static PublicadorFake QueFunciona() => new(null, false);
    public static PublicadorFake QueFalla(Exception excepcion) => new(excepcion, false);
    public static PublicadorFake BrokerCaido() => QueFalla(new InvalidOperationException("Broker caído (simulado)."));
    public static PublicadorFake QueSeCuelga() => new(null, true);

    public async Task PublicarAsync<T>(string routingKey, T evento, CancellationToken ct = default)
    {
        Intentos++;

        if (_colgarse)
            await Task.Delay(Timeout.Infinite, ct);

        if (_excepcion is not null)
            throw _excepcion;

        Publicados.Add((routingKey, evento!));
    }
}
