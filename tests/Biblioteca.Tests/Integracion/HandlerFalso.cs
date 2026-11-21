namespace Biblioteca.Tests.Integracion;

/// <summary>
/// HttpMessageHandler de prueba: en lugar de ir a la red, ejecuta la función recibida.
/// Permite simular cualquier respuesta (200, 404, 5xx), fallos de conexión o esperas indefinidas.
/// </summary>
internal sealed class HandlerFalso : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    public HandlerFalso(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    public List<HttpRequestMessage> Peticiones { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Peticiones.Add(request);
        return _responder(request, cancellationToken);
    }
}
