using Prestamos.Api.Integrations.Mensajeria.Publishers;

namespace Biblioteca.Tests.Integracion;

/// <summary>
/// Fake de IEventPublisher: no usa RabbitMQ. Registra lo publicado, o falla / se cuelga a pedido
/// para probar que el caso de uso no revierte el préstamo cuando la publicación no se completa.
/// </summary>
internal sealed class EventPublisherFake : IEventPublisher
{
    private readonly Exception? _excepcion;
    private readonly bool _colgarse;

    private EventPublisherFake(Exception? excepcion, bool colgarse)
    {
        _excepcion = excepcion;
        _colgarse = colgarse;
    }

    public List<(string RoutingKey, object Evento)> Publicados { get; } = new();
    public int Intentos { get; private set; }

    public static EventPublisherFake QueFunciona() => new(null, false);
    public static EventPublisherFake QueFalla(Exception excepcion) => new(excepcion, false);

    /// <summary>Nunca termina por sí solo: solo sale cuando se cancela el token (p. ej. por timeout).</summary>
    public static EventPublisherFake QueSeCuelga() => new(null, true);

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
