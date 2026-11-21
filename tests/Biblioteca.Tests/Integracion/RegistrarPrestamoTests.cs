using Microsoft.AspNetCore.Http;
using Prestamos.Api.Aplicacion;
using Prestamos.Api.Contracts;
using Prestamos.Api.Integrations;
using Prestamos.Application.Contracts;
using Prestamos.Domain.Exceptions;
using Prestamos.Infrastructure.Repository;

namespace Biblioteca.Tests.Integracion;

/// <summary>
/// Pruebas del caso de uso RegistrarPrestamo con un LibroConsultaClientFake (sin HTTP)
/// y el repositorio en memoria. Cubre la matriz de resultados y su traducción a HTTP.
/// </summary>
public class RegistrarPrestamoTests
{
    private const int LibroId = 5;

    private static RegistrarPrestamoComando ComandoValido(string? lector = "est-001") =>
        new(LibroId, lector, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 27));

    private static LibroConsultaDto Libro(bool activo) =>
        new(LibroId, "LIB-DDD-01", "Domain-Driven Design", activo);

    private static async Task<(ResultadoRegistrarPrestamo Resultado, PrestamoRepositoryEnMemoria Repositorio)> Ejecutar(
        LibroConsultaClientFake catalogo)
    {
        var repositorio = new PrestamoRepositoryEnMemoria();
        var casoDeUso = new RegistrarPrestamo(catalogo, repositorio);

        var resultado = await casoDeUso.EjecutarAsync(ComandoValido(), CancellationToken.None);
        return (resultado, repositorio);
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

        var (resultado, repositorio) = await Ejecutar(catalogo);

        var creado = Assert.IsType<ResultadoRegistrarPrestamo.Creado>(resultado);
        Assert.Equal(LibroId, creado.Prestamo.LibroId);
        Assert.Equal("EST-001", creado.Prestamo.Lector);
        Assert.Equal("Activo", creado.Prestamo.Estado);

        Assert.Single(repositorio.Listar());
        Assert.Equal([LibroId], catalogo.Consultas);
        Assert.Equal(StatusCodes.Status201Created, CodigoHttp(resultado));
    }

    [Fact]
    public async Task Libro_inactivo_no_registra_y_responde_409()
    {
        var (resultado, repositorio) = await Ejecutar(LibroConsultaClientFake.QueDevuelve(Libro(activo: false)));

        Assert.IsType<ResultadoRegistrarPrestamo.LibroNoDisponible>(resultado);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(StatusCodes.Status409Conflict, CodigoHttp(resultado));
        AssertCuerpoDeError(resultado);
    }

    [Fact]
    public async Task Libro_inexistente_no_registra_y_responde_422()
    {
        var (resultado, repositorio) = await Ejecutar(LibroConsultaClientFake.QueDevuelve(null));

        Assert.IsType<ResultadoRegistrarPrestamo.LibroNoExiste>(resultado);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, CodigoHttp(resultado));
        AssertCuerpoDeError(resultado);
    }

    [Fact]
    public async Task Catalogo_no_disponible_no_registra_y_responde_503()
    {
        var catalogo = LibroConsultaClientFake.QueLanza(new CatalogoNoDisponibleException("no disponible"));

        var (resultado, repositorio) = await Ejecutar(catalogo);

        Assert.IsType<ResultadoRegistrarPrestamo.CatalogoNoDisponible>(resultado);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, CodigoHttp(resultado));
        AssertCuerpoDeError(resultado);
    }

    [Fact]
    public async Task Catalogo_con_timeout_no_registra_y_responde_504()
    {
        var catalogo = LibroConsultaClientFake.QueLanza(new CatalogoTimeoutException("timeout"));

        var (resultado, repositorio) = await Ejecutar(catalogo);

        Assert.IsType<ResultadoRegistrarPrestamo.CatalogoTimeout>(resultado);
        Assert.Empty(repositorio.Listar());
        Assert.Equal(StatusCodes.Status504GatewayTimeout, CodigoHttp(resultado));
        AssertCuerpoDeError(resultado);
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
        var casoDeUso = new RegistrarPrestamo(catalogo, repositorio);

        await Assert.ThrowsAsync<ValidacionDominioException>(
            () => casoDeUso.EjecutarAsync(ComandoValido(lectorVacio), CancellationToken.None));

        Assert.Empty(catalogo.Consultas);
        Assert.Empty(repositorio.Listar());
    }
}
