using System.Diagnostics;
using Biblioteca.Tests.Integracion;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Prestamos.Api.Aplicacion;
using Prestamos.Application.Contracts;
using Prestamos.Application.Exceptions;
using Prestamos.Api.Integrations.Mensajeria.Events;
using Prestamos.Infrastructure.Repository;

namespace Biblioteca.Tests.Mensajeria;

/// <summary>
/// Publicación de PrestamoRegistrado desde el caso de uso RegistrarPrestamo,
/// con LibroConsultaClientFake (sin HTTP) y PublicadorFake (sin RabbitMQ).
/// </summary>
public class RegistrarPrestamoPublicacionTests
{
    private const int LibroId = 5;

    private static RegistrarPrestamoComando ComandoValido() =>
        new(LibroId, "est-001", new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 27));

    private static LibroConsultaDto Libro(bool activo) =>
        new(LibroId, "LIB-DDD-01", "Domain-Driven Design", activo);

    private static async Task<(ResultadoRegistrarPrestamo Resultado, PrestamoRepositoryEnMemoria Repositorio)> Ejecutar(
        LibroConsultaClientFake catalogo, PublicadorFake publicador)
    {
        var repositorio = new PrestamoRepositoryEnMemoria();
        var casoDeUso = new RegistrarPrestamo(catalogo, repositorio, publicador, NullLogger<RegistrarPrestamo>.Instance);

        var resultado = await casoDeUso.EjecutarAsync(ComandoValido(), CancellationToken.None);
        return (resultado, repositorio);
    }

    [Fact]
    public async Task Prestamo_valido_publica_UN_evento_prestamo_registrado_con_los_ids_correctos()
    {
        var publicador = PublicadorFake.QueFunciona();

        var (resultado, repositorio) = await Ejecutar(LibroConsultaClientFake.QueDevuelve(Libro(activo: true)), publicador);

        var creado = Assert.IsType<ResultadoRegistrarPrestamo.Creado>(resultado);
        Assert.True(creado.EventoPublicado);

        var (routingKey, objeto) = Assert.Single(publicador.Publicados);
        Assert.Equal("prestamo.registrado", routingKey);

        var evento = Assert.IsType<PrestamoRegistrado>(objeto);
        var prestamoGuardado = Assert.Single(repositorio.Listar());
        Assert.Equal(creado.EventoId, evento.EventoId);
        Assert.Equal(prestamoGuardado.Id, evento.PrestamoId);
        Assert.Equal(LibroId, evento.LibroId);
        Assert.Equal(1, evento.Version);

        var http = Assert.IsType<ResultadoConEncabezados>(PrestamoHttp.Traducir(resultado));
        Assert.Equal(StatusCodes.Status201Created, http.StatusCode);
        Assert.Equal(evento.EventoId.ToString(), http.Encabezados[PrestamoHttp.EncabezadoEventoId]);
        Assert.Equal("true", http.Encabezados[PrestamoHttp.EncabezadoEventoPublicado]);
    }

    public static TheoryData<string> CasosSinPrestamo => ["libro inactivo", "libro inexistente", "Catálogo caído", "Catálogo timeout"];

    [Theory]
    [MemberData(nameof(CasosSinPrestamo))]
    public async Task Sin_prestamo_registrado_no_se_publica_nada(string caso)
    {
        var catalogo = caso switch
        {
            "libro inactivo" => LibroConsultaClientFake.QueDevuelve(Libro(activo: false)),
            "libro inexistente" => LibroConsultaClientFake.QueDevuelve(null),
            "Catálogo caído" => LibroConsultaClientFake.QueLanza(new CatalogoNoDisponibleException("caído")),
            "Catálogo timeout" => LibroConsultaClientFake.QueLanza(new CatalogoTimeoutException("timeout")),
            _ => throw new ArgumentOutOfRangeException(nameof(caso))
        };
        var publicador = PublicadorFake.QueFunciona();

        var (resultado, repositorio) = await Ejecutar(catalogo, publicador);

        Assert.IsNotType<ResultadoRegistrarPrestamo.Creado>(resultado);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(0, publicador.Intentos);
        Assert.Empty(publicador.Publicados);
    }

    [Fact]
    public async Task Con_el_broker_caido_el_prestamo_queda_guardado_EventoPublicado_false_y_HTTP_201()
    {
        var publicador = PublicadorFake.BrokerCaido();

        var (resultado, repositorio) = await Ejecutar(LibroConsultaClientFake.QueDevuelve(Libro(activo: true)), publicador);

        var creado = Assert.IsType<ResultadoRegistrarPrestamo.Creado>(resultado);
        Assert.False(creado.EventoPublicado);
        Assert.Single(repositorio.Listar());   // el préstamo no se revierte
        Assert.Equal(1, publicador.Intentos);
        Assert.Empty(publicador.Publicados);

        var http = Assert.IsType<ResultadoConEncabezados>(PrestamoHttp.Traducir(resultado));
        Assert.Equal(StatusCodes.Status201Created, http.StatusCode);
        Assert.Equal(creado.EventoId.ToString(), http.Encabezados[PrestamoHttp.EncabezadoEventoId]);
        Assert.Equal("false", http.Encabezados[PrestamoHttp.EncabezadoEventoPublicado]);
    }

    [Fact]
    public async Task Si_el_broker_no_responde_se_corta_a_los_4_segundos_y_el_prestamo_se_conserva()
    {
        var reloj = Stopwatch.StartNew();

        var (resultado, repositorio) = await Ejecutar(
            LibroConsultaClientFake.QueDevuelve(Libro(activo: true)), PublicadorFake.QueSeCuelga());

        reloj.Stop();
        var creado = Assert.IsType<ResultadoRegistrarPrestamo.Creado>(resultado);
        Assert.False(creado.EventoPublicado);
        Assert.Single(repositorio.Listar());
        Assert.InRange(reloj.Elapsed, TimeSpan.FromSeconds(3.9), TimeSpan.FromSeconds(6));
    }
}
