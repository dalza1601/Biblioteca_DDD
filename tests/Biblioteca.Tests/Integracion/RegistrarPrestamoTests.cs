using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Prestamos.Api.Aplicacion;
using Prestamos.Api.Contracts;
using Prestamos.Api.Integrations.Catalogo.Contracts;
using Prestamos.Api.Integrations.Catalogo.Exceptions;
using Prestamos.Api.Integrations.Mensajeria.Events;
using Prestamos.Application.Contracts;
using Prestamos.Domain.Exceptions;
using Prestamos.Infrastructure.Repository;

namespace Biblioteca.Tests.Integracion;

/// <summary>
/// Pruebas del caso de uso RegistrarPrestamo con un LibroConsultaClientFake (sin HTTP),
/// un EventPublisherFake (sin RabbitMQ) y el repositorio en memoria.
/// Cubre la matriz de resultados, su traducción a HTTP y la publicación del evento.
/// </summary>
public class RegistrarPrestamoTests
{
    private const int LibroId = 5;

    private static RegistrarPrestamoComando ComandoValido(string? lector = "est-001") =>
        new(LibroId, lector, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 27));

    private static LibroConsultaDto Libro(bool activo) =>
        new(LibroId, "LIB-DDD-01", "Domain-Driven Design", activo);

    private static RegistrarPrestamo CrearCasoDeUso(
        LibroConsultaClientFake catalogo, PrestamoRepositoryEnMemoria repositorio, EventPublisherFake publicador) =>
        new(catalogo, repositorio, publicador, NullLogger<RegistrarPrestamo>.Instance);

    private static async Task<(ResultadoRegistrarPrestamo Resultado, PrestamoRepositoryEnMemoria Repositorio, EventPublisherFake Publicador)> Ejecutar(
        LibroConsultaClientFake catalogo, EventPublisherFake? publicador = null)
    {
        var repositorio = new PrestamoRepositoryEnMemoria();
        publicador ??= EventPublisherFake.QueFunciona();

        var resultado = await CrearCasoDeUso(catalogo, repositorio, publicador)
            .EjecutarAsync(ComandoValido(), CancellationToken.None);
        return (resultado, repositorio, publicador);
    }

    private static int CodigoHttp(ResultadoRegistrarPrestamo resultado) =>
        Assert.IsAssignableFrom<IStatusCodeHttpResult>(PrestamoHttp.Traducir(resultado)).StatusCode!.Value;

    private static void AssertCuerpoDeError(ResultadoRegistrarPrestamo resultado)
    {
        var cuerpo = Assert.IsAssignableFrom<IValueHttpResult>(PrestamoHttp.Traducir(resultado)).Value;
        var error = Assert.IsType<ErrorResponse>(cuerpo);
        Assert.False(string.IsNullOrWhiteSpace(error.Error));
    }

    // ---------- Matriz de resultados ----------

    [Fact]
    public async Task Libro_activo_registra_el_prestamo_y_responde_201()
    {
        var catalogo = LibroConsultaClientFake.QueDevuelve(Libro(activo: true));

        var (resultado, repositorio, _) = await Ejecutar(catalogo);

        var creado = Assert.IsType<ResultadoRegistrarPrestamo.Creado>(resultado);
        Assert.Equal(LibroId, creado.Prestamo.LibroId);
        Assert.Equal("EST-001", creado.Prestamo.Lector);
        Assert.Equal("Activo", creado.Prestamo.Estado);

        Assert.Single(repositorio.Listar());
        Assert.Equal([LibroId], catalogo.Consultas);
        Assert.Equal(StatusCodes.Status201Created, CodigoHttp(resultado));
    }

    [Fact]
    public async Task Libro_inactivo_no_registra_no_publica_y_responde_409()
    {
        var (resultado, repositorio, publicador) = await Ejecutar(LibroConsultaClientFake.QueDevuelve(Libro(activo: false)));

        Assert.IsType<ResultadoRegistrarPrestamo.LibroNoDisponible>(resultado);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(0, publicador.Intentos);
        Assert.Equal(StatusCodes.Status409Conflict, CodigoHttp(resultado));
        AssertCuerpoDeError(resultado);
    }

    [Fact]
    public async Task Libro_inexistente_no_registra_no_publica_y_responde_422()
    {
        var (resultado, repositorio, publicador) = await Ejecutar(LibroConsultaClientFake.QueDevuelve(null));

        Assert.IsType<ResultadoRegistrarPrestamo.LibroNoExiste>(resultado);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(0, publicador.Intentos);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, CodigoHttp(resultado));
        AssertCuerpoDeError(resultado);
    }

    [Fact]
    public async Task Catalogo_no_disponible_no_registra_no_publica_y_responde_503()
    {
        var catalogo = LibroConsultaClientFake.QueLanza(new CatalogoNoDisponibleException("no disponible"));

        var (resultado, repositorio, publicador) = await Ejecutar(catalogo);

        Assert.IsType<ResultadoRegistrarPrestamo.CatalogoNoDisponible>(resultado);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(0, publicador.Intentos);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, CodigoHttp(resultado));
        AssertCuerpoDeError(resultado);
    }

    [Fact]
    public async Task Catalogo_con_timeout_no_registra_no_publica_y_responde_504()
    {
        var catalogo = LibroConsultaClientFake.QueLanza(new CatalogoTimeoutException("timeout"));

        var (resultado, repositorio, publicador) = await Ejecutar(catalogo);

        Assert.IsType<ResultadoRegistrarPrestamo.CatalogoTimeout>(resultado);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(0, publicador.Intentos);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, CodigoHttp(resultado));
        AssertCuerpoDeError(resultado);
    }

    // ---------- Publicación del evento PrestamoRegistrado ----------

    [Fact]
    public async Task Prestamo_creado_publica_PrestamoRegistrado_y_responde_201_con_encabezados()
    {
        var (resultado, repositorio, publicador) = await Ejecutar(LibroConsultaClientFake.QueDevuelve(Libro(activo: true)));

        var creado = Assert.IsType<ResultadoRegistrarPrestamo.Creado>(resultado);
        Assert.True(creado.EventoPublicado);

        var (routingKey, objeto) = Assert.Single(publicador.Publicados);
        var evento = Assert.IsType<PrestamoRegistrado>(objeto);
        Assert.Equal(PrestamoRegistrado.RoutingKey, routingKey);
        Assert.Equal(creado.EventoId, evento.EventoId);
        Assert.Equal(creado.Prestamo.Id, evento.PrestamoId);
        Assert.Equal(LibroId, evento.LibroId);
        Assert.Single(repositorio.Listar());

        var http = Assert.IsType<ResultadoConEncabezados>(PrestamoHttp.Traducir(resultado));
        Assert.Equal(StatusCodes.Status201Created, http.StatusCode);
        Assert.Equal(creado.EventoId.ToString(), http.Encabezados[PrestamoHttp.EncabezadoEventoId]);
        Assert.Equal("true", http.Encabezados[PrestamoHttp.EncabezadoEventoPublicado]);
    }

    [Fact]
    public async Task Si_publicar_falla_el_prestamo_se_conserva_y_EventoPublicado_es_false()
    {
        var publicador = EventPublisherFake.QueFalla(new InvalidOperationException("RabbitMQ caído"));

        var (resultado, repositorio, _) = await Ejecutar(LibroConsultaClientFake.QueDevuelve(Libro(activo: true)), publicador);

        var creado = Assert.IsType<ResultadoRegistrarPrestamo.Creado>(resultado);
        Assert.False(creado.EventoPublicado);
        Assert.Single(repositorio.Listar()); // no se revierte
        Assert.Equal(1, publicador.Intentos);

        var http = Assert.IsType<ResultadoConEncabezados>(PrestamoHttp.Traducir(resultado));
        Assert.Equal(StatusCodes.Status201Created, http.StatusCode);
        Assert.Equal(creado.EventoId.ToString(), http.Encabezados[PrestamoHttp.EncabezadoEventoId]);
        Assert.Equal("false", http.Encabezados[PrestamoHttp.EncabezadoEventoPublicado]);
    }

    [Fact]
    public async Task Si_publicar_no_termina_se_corta_a_los_4_segundos_y_el_prestamo_se_conserva()
    {
        var publicador = EventPublisherFake.QueSeCuelga();
        var reloj = Stopwatch.StartNew();

        var (resultado, repositorio, _) = await Ejecutar(LibroConsultaClientFake.QueDevuelve(Libro(activo: true)), publicador);

        reloj.Stop();
        var creado = Assert.IsType<ResultadoRegistrarPrestamo.Creado>(resultado);
        Assert.False(creado.EventoPublicado);
        Assert.Single(repositorio.Listar());
        Assert.InRange(reloj.Elapsed, TimeSpan.FromSeconds(3.9), TimeSpan.FromSeconds(6));
    }

    // ---------- Validación barata antes de consultar a Catálogo ----------

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Lector_vacio_lanza_ValidacionDominioException_sin_consultar_a_Catalogo(string? lectorVacio)
    {
        var catalogo = LibroConsultaClientFake.QueDevuelve(Libro(activo: true));
        var repositorio = new PrestamoRepositoryEnMemoria();
        var publicador = EventPublisherFake.QueFunciona();
        var casoDeUso = CrearCasoDeUso(catalogo, repositorio, publicador);

        await Assert.ThrowsAsync<ValidacionDominioException>(
            () => casoDeUso.EjecutarAsync(ComandoValido(lectorVacio), CancellationToken.None));

        Assert.Empty(catalogo.Consultas);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(0, publicador.Intentos);
    }
}
