namespace Prestamos.Api.Integrations.Mensajeria.Publishers;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ) · <b>Tipo:</b> Interfaz del publicador de eventos (puerto)</para>
/// Publica un evento de integración con su routing key. Lanza excepción si no se pudo publicar.
/// </summary>
public interface IEventPublisher
{
    Task PublicarAsync<T>(string routingKey, T evento, CancellationToken ct = default);
}
