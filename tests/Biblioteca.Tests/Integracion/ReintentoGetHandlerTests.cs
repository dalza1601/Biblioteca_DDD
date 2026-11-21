using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Prestamos.Api.Integrations.Catalogo.Clients;
using Prestamos.Api.Integrations.Catalogo.Exceptions;
using Prestamos.Api.Integrations.Catalogo.Handlers;

namespace Biblioteca.Tests.Integracion;

/// <summary>
/// Pruebas de ReintentoGetHandler encadenado delante de un HandlerFalso (sin red).
/// La espera entre intentos se sustituye por una que solo registra el retardo pedido.
/// </summary>
public class ReintentoGetHandlerTests
{
    private const string LibroJson = """{"id":1,"codigo":"LIB-DDD-01","titulo":"Domain-Driven Design","activo":true}""";

    private readonly List<TimeSpan> _esperas = new();

    private HttpClient CrearHttpClient(HandlerFalso handlerFalso, int maxIntentos = ReintentoGetHandler.MaximoIntentos)
    {
        var reintentos = new ReintentoGetHandler(
            maxIntentos,
            NullLogger<ReintentoGetHandler>.Instance,
            esperar: (retardo, _) => { _esperas.Add(retardo); return Task.CompletedTask; })
        {
            InnerHandler = handlerFalso
        };

        return new HttpClient(reintentos) { BaseAddress = new Uri("http://catalogo.test/") };
    }

    private static Task<HttpResponseMessage> Respuesta(HttpStatusCode codigo, string? json = null) =>
        Task.FromResult(new HttpResponseMessage(codigo)
        {
            Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json")
        });

    private static LibroConsultaClient CrearCliente(HttpClient httpClient) =>
        new(httpClient, NullLogger<LibroConsultaClient>.Instance);

    [Fact]
    public async Task GET_se_recupera_tras_2_fallos_y_devuelve_el_libro()
    {
        var llamada = 0;
        var handlerFalso = new HandlerFalso((_, _) => ++llamada switch
        {
            1 => Respuesta(HttpStatusCode.ServiceUnavailable),                                 // fallo 1: 5xx
            2 => Task.FromException<HttpResponseMessage>(new HttpRequestException("caído")), // fallo 2: conexión
            _ => Respuesta(HttpStatusCode.OK, LibroJson)                                      // 3.º intento: OK
        });
        var cliente = CrearCliente(CrearHttpClient(handlerFalso));

        var libro = await cliente.ObtenerLibroAsync(1, CancellationToken.None);

        Assert.NotNull(libro);
        Assert.Equal("Domain-Driven Design", libro.Titulo);
        Assert.Equal(3, handlerFalso.Peticiones.Count);
        Assert.Equal([TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(400)], _esperas); // backoff exponencial
    }

    [Theory]
    [InlineData(true)]  // 500 persistente
    [InlineData(false)] // HttpRequestException persistente
    public async Task GET_con_fallo_persistente_hace_exactamente_3_llamadas_y_lanza_CatalogoNoDisponibleException(bool responde500)
    {
        var handlerFalso = new HandlerFalso((_, _) => responde500
            ? Respuesta(HttpStatusCode.InternalServerError)
            : Task.FromException<HttpResponseMessage>(new HttpRequestException("caído")));
        var cliente = CrearCliente(CrearHttpClient(handlerFalso));

        await Assert.ThrowsAsync<CatalogoNoDisponibleException>(
            () => cliente.ObtenerLibroAsync(1, CancellationToken.None));

        Assert.Equal(3, handlerFalso.Peticiones.Count);
        Assert.Equal(2, _esperas.Count); // espera entre 1→2 y 2→3, no después del último
    }

    [Theory]
    [InlineData(true)]  // responde 503
    [InlineData(false)] // HttpRequestException
    public async Task POST_nunca_se_repite(bool responde503)
    {
        var handlerFalso = new HandlerFalso((_, _) => responde503
            ? Respuesta(HttpStatusCode.ServiceUnavailable)
            : Task.FromException<HttpResponseMessage>(new HttpRequestException("caído")));
        using var httpClient = CrearHttpClient(handlerFalso);

        if (responde503)
        {
            using var respuesta = await httpClient.PostAsync("libros", new StringContent("{}"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        }
        else
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => httpClient.PostAsync("libros", new StringContent("{}")));
        }

        Assert.Single(handlerFalso.Peticiones);
        Assert.Empty(_esperas);
    }

    [Fact]
    public async Task GET_con_404_no_se_reintenta_porque_no_es_un_fallo_del_servidor()
    {
        var handlerFalso = new HandlerFalso((_, _) => Respuesta(HttpStatusCode.NotFound));
        var cliente = CrearCliente(CrearHttpClient(handlerFalso));

        var libro = await cliente.ObtenerLibroAsync(999, CancellationToken.None);

        Assert.Null(libro);
        Assert.Single(handlerFalso.Peticiones);
    }

    [Fact]
    public async Task Con_un_solo_intento_configurado_no_hay_reintentos()
    {
        var handlerFalso = new HandlerFalso((_, _) => Respuesta(HttpStatusCode.InternalServerError));
        var cliente = CrearCliente(CrearHttpClient(handlerFalso, maxIntentos: 1));

        await Assert.ThrowsAsync<CatalogoNoDisponibleException>(
            () => cliente.ObtenerLibroAsync(1, CancellationToken.None));

        Assert.Single(handlerFalso.Peticiones);
    }
}
