using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Prestamos.Api.Integrations.Catalogo.Clients;
using Prestamos.Api.Integrations.Catalogo.Contracts;
using Prestamos.Api.Integrations.Catalogo.Exceptions;

namespace Biblioteca.Tests.Integracion;

/// <summary>
/// Pruebas del cliente HTTP hacia Catálogo con un HandlerFalso: no se usa la red ni servicios reales.
/// </summary>
public class LibroConsultaClientTests
{
    private static readonly Uri UrlCatalogoFalsa = new("http://catalogo.test/");

    private static (LibroConsultaClient Cliente, HandlerFalso Handler) CrearCliente(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder,
        TimeSpan? timeout = null)
    {
        var handler = new HandlerFalso(responder);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = UrlCatalogoFalsa,
            Timeout = timeout ?? TimeSpan.FromSeconds(10)
        };

        return (new LibroConsultaClient(httpClient, NullLogger<LibroConsultaClient>.Instance), handler);
    }

    private static Task<HttpResponseMessage> Responder(HttpStatusCode codigo, string? json = null) =>
        Task.FromResult(new HttpResponseMessage(codigo)
        {
            Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json")
        });

    [Fact]
    public async Task Catalogo_responde_200_devuelve_el_libro_leido_del_JSON()
    {
        var (cliente, handler) = CrearCliente((_, _) => Responder(HttpStatusCode.OK,
            """{"id":7,"codigo":"LIB-DDD-01","titulo":"Domain-Driven Design","activo":true}"""));

        var libro = await cliente.ObtenerLibroAsync(7, CancellationToken.None);

        Assert.Equal(new LibroConsultaDto(7, "LIB-DDD-01", "Domain-Driven Design", true), libro);

        var peticion = Assert.Single(handler.Peticiones);
        Assert.Equal(HttpMethod.Get, peticion.Method);
        Assert.Equal(new Uri("http://catalogo.test/libros/7"), peticion.RequestUri);
        Assert.Null(peticion.Headers.Authorization); // no reenvía tokens ni credenciales
    }

    [Fact]
    public async Task Catalogo_responde_404_devuelve_null()
    {
        var (cliente, _) = CrearCliente((_, _) => Responder(HttpStatusCode.NotFound,
            """{"error":"Libro no encontrado"}"""));

        var libro = await cliente.ObtenerLibroAsync(999, CancellationToken.None);

        Assert.Null(libro);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadRequest)]   // código inesperado
    [InlineData(HttpStatusCode.NoContent)]    // código inesperado (2xx distinto de 200)
    public async Task Catalogo_responde_5xx_o_codigo_inesperado_lanza_CatalogoNoDisponibleException(HttpStatusCode codigo)
    {
        var (cliente, _) = CrearCliente((_, _) => Responder(codigo));

        await Assert.ThrowsAsync<CatalogoNoDisponibleException>(
            () => cliente.ObtenerLibroAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task Fallo_de_conexion_HttpRequestException_lanza_CatalogoNoDisponibleException()
    {
        var (cliente, _) = CrearCliente((_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("Conexión rechazada")));

        var excepcion = await Assert.ThrowsAsync<CatalogoNoDisponibleException>(
            () => cliente.ObtenerLibroAsync(1, CancellationToken.None));

        Assert.IsType<HttpRequestException>(excepcion.InnerException);
        Assert.DoesNotContain("rechazada", excepcion.Message); // el detalle técnico no sale en el mensaje
    }

    [Fact]
    public async Task Catalogo_no_responde_dentro_del_timeout_lanza_CatalogoTimeoutException()
    {
        // HttpClient.Timeout de 100 ms y un handler que espera indefinidamente.
        var (cliente, _) = CrearCliente(
            async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
            timeout: TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAsync<CatalogoTimeoutException>(
            () => cliente.ObtenerLibroAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task Cancelacion_del_cliente_se_propaga_y_no_es_CatalogoTimeoutException()
    {
        // Timeout amplio (10 s): la única cancelación posible es la del cliente.
        var (cliente, _) = CrearCliente(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var cancelacion = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var excepcion = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cliente.ObtenerLibroAsync(1, cancelacion.Token));

        Assert.IsNotType<CatalogoTimeoutException>(excepcion);
        Assert.True(cancelacion.IsCancellationRequested);
    }
}
