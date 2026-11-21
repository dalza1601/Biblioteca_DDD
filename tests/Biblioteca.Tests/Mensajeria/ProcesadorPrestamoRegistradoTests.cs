using System.Text;
using Catalogo.Api.Mensajeria.Processing;
using Catalogo.Api.Mensajeria.Stores;
using Microsoft.Extensions.Logging.Abstractions;

namespace Biblioteca.Tests.Mensajeria;

/// <summary>
/// Pruebas de ProcesadorPrestamoRegistrado (Catálogo) solo con bytes: sin RabbitMQ.
/// </summary>
public class ProcesadorPrestamoRegistradoTests
{
    private readonly ContadorPrestamosPorLibro _contador = new();
    private readonly Cuarentena _cuarentena = new();
    private readonly ProcesadorPrestamoRegistrado _procesador;

    public ProcesadorPrestamoRegistradoTests()
    {
        _procesador = new ProcesadorPrestamoRegistrado(_contador, _cuarentena, NullLogger<ProcesadorPrestamoRegistrado>.Instance);
    }

    private static byte[] Mensaje(Guid eventoId, int prestamoId = 10, int libroId = 3) =>
        Encoding.UTF8.GetBytes(
            $$"""{"eventoId":"{{eventoId}}","prestamoId":{{prestamoId}},"libroId":{{libroId}},"fechaRegistroUtc":"2026-11-21T15:30:00Z","version":1}""");

    private int PrestamosDelLibro(int libroId) =>
        _contador.Instantanea().SingleOrDefault(par => par.LibroId == libroId).Prestamos;

    [Fact]
    public void Mensaje_nuevo_se_procesa_y_suma_un_prestamo_al_libro()
    {
        var resultado = _procesador.Procesar(Mensaje(Guid.NewGuid(), libroId: 3));

        Assert.Equal(ResultadoProcesamiento.Procesado, resultado);
        Assert.Equal(1, PrestamosDelLibro(3));
        Assert.Empty(_cuarentena.Listar());
    }

    [Fact]
    public void Mismo_eventoId_dos_veces_es_Duplicado_y_no_duplica_el_conteo()
    {
        var cuerpo = Mensaje(Guid.NewGuid(), libroId: 3);

        var primero = _procesador.Procesar(cuerpo);
        var segundo = _procesador.Procesar(cuerpo);

        Assert.Equal(ResultadoProcesamiento.Procesado, primero);
        Assert.Equal(ResultadoProcesamiento.Duplicado, segundo);
        Assert.Equal(1, PrestamosDelLibro(3));
    }

    [Fact]
    public void Eventos_distintos_del_mismo_libro_se_cuentan_todos()
    {
        Assert.Equal(ResultadoProcesamiento.Procesado, _procesador.Procesar(Mensaje(Guid.NewGuid(), prestamoId: 1, libroId: 3)));
        Assert.Equal(ResultadoProcesamiento.Procesado, _procesador.Procesar(Mensaje(Guid.NewGuid(), prestamoId: 2, libroId: 3)));
        Assert.Equal(ResultadoProcesamiento.Procesado, _procesador.Procesar(Mensaje(Guid.NewGuid(), prestamoId: 3, libroId: 8)));

        Assert.Equal(2, PrestamosDelLibro(3));
        Assert.Equal(1, PrestamosDelLibro(8));
    }

    [Theory]
    [InlineData("", "JSON")]
    [InlineData("""{"eventoId": """, "JSON")]
    [InlineData("null", "vacío")]
    [InlineData("""{"eventoId":"00000000-0000-0000-0000-000000000000","prestamoId":1,"libroId":3,"fechaRegistroUtc":"2026-11-21T15:30:00Z","version":1}""", "eventoId")]
    [InlineData("""{"eventoId":"aaaaaaaa-0000-0000-0000-000000000001","prestamoId":0,"libroId":3,"fechaRegistroUtc":"2026-11-21T15:30:00Z","version":1}""", "prestamoId")]
    [InlineData("""{"eventoId":"aaaaaaaa-0000-0000-0000-000000000002","prestamoId":1,"fechaRegistroUtc":"2026-11-21T15:30:00Z","version":1}""", "libroId")]
    [InlineData("""{"eventoId":"aaaaaaaa-0000-0000-0000-000000000003","prestamoId":1,"libroId":3,"version":1}""", "fechaRegistroUtc")]
    [InlineData("""{"eventoId":"aaaaaaaa-0000-0000-0000-000000000004","prestamoId":1,"libroId":3,"fechaRegistroUtc":"2026-11-21T15:30:00Z"}""", "version")]
    [InlineData("""{"eventoId":"aaaaaaaa-0000-0000-0000-000000000005","prestamoId":"diez","libroId":3,"fechaRegistroUtc":"2026-11-21T15:30:00Z","version":1}""", "JSON")]
    public void Contrato_invalido_va_a_cuarentena_con_su_motivo_y_no_cuenta(string json, string motivoContiene)
    {
        var resultado = _procesador.Procesar(Encoding.UTF8.GetBytes(json));

        Assert.Equal(ResultadoProcesamiento.Invalido, resultado);
        var enCuarentena = Assert.Single(_cuarentena.Listar());
        Assert.Contains(motivoContiene, enCuarentena.Motivo);
        Assert.Equal(json, enCuarentena.Contenido);
        Assert.Empty(_contador.Instantanea());
    }

    [Fact]
    public void Version_2_va_a_cuarentena_como_no_soportada()
    {
        var json = """{"eventoId":"aaaaaaaa-0000-0000-0000-000000000009","prestamoId":1,"libroId":3,"fechaRegistroUtc":"2026-11-21T15:30:00Z","version":2}""";

        var resultado = _procesador.Procesar(Encoding.UTF8.GetBytes(json));

        Assert.Equal(ResultadoProcesamiento.Invalido, resultado);
        var enCuarentena = Assert.Single(_cuarentena.Listar());
        Assert.Contains("Versión 2 no soportada", enCuarentena.Motivo);
        Assert.Empty(_contador.Instantanea());
    }

    [Fact]
    public void Reentrega_tras_fallo_antes_del_ACK_aplica_el_efecto_una_sola_vez()
    {
        var cuerpo = Mensaje(Guid.NewGuid(), libroId: 3);

        // 1.ª entrega: se procesa, pero el consumidor "se cae" antes del ACK.
        var primeraEntrega = _procesador.Procesar(cuerpo, reentregado: false);
        // RabbitMQ reentrega el mismo mensaje marcado como Redelivered (dos veces).
        var reentrega1 = _procesador.Procesar(cuerpo, reentregado: true);
        var reentrega2 = _procesador.Procesar(cuerpo, reentregado: true);

        Assert.Equal(ResultadoProcesamiento.Procesado, primeraEntrega);
        Assert.Equal(ResultadoProcesamiento.Duplicado, reentrega1);
        Assert.Equal(ResultadoProcesamiento.Duplicado, reentrega2);
        Assert.Equal(1, PrestamosDelLibro(3));
    }

    [Fact]
    public async Task Entregas_simultaneas_del_mismo_evento_cuentan_una_sola_vez()
    {
        var cuerpo = Mensaje(Guid.NewGuid(), libroId: 3);

        var resultados = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(_ => Task.Run(() => _procesador.Procesar(cuerpo, reentregado: true))));

        Assert.Equal(1, resultados.Count(r => r == ResultadoProcesamiento.Procesado));
        Assert.Equal(49, resultados.Count(r => r == ResultadoProcesamiento.Duplicado));
        Assert.Equal(1, PrestamosDelLibro(3));
    }
}
