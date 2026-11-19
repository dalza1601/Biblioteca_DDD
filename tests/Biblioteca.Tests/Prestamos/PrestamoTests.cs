using Prestamos.Domain.Entities;
using Prestamos.Domain.Enums;
using Prestamos.Domain.Exceptions;
using Prestamos.Domain.ValueObjects;

namespace Biblioteca.Tests.Prestamos;

public class PrestamoTests
{
    private static readonly DateOnly Inicio = new(2026, 11, 18);
    private static readonly DateOnly Limite = new(2026, 11, 25);

    private static Prestamo RegistrarPrestamoValido() =>
        Prestamo.Registrar(1, CodigoLector.Crear("EST-001"), Inicio, Limite);

    // ---------- Registrar ----------

    [Fact]
    public void Un_prestamo_recien_registrado_inicia_en_Activo_y_sin_fecha_de_devolucion()
    {
        var prestamo = RegistrarPrestamoValido();

        Assert.Equal(EstadoPrestamo.Activo, prestamo.Estado);
        Assert.Null(prestamo.FechaDevolucion);
    }

    [Fact]
    public void Un_prestamo_solo_guarda_el_LibroId_recibido()
    {
        var prestamo = Prestamo.Registrar(42, CodigoLector.Crear("EST-001"), Inicio, Limite);

        Assert.Equal(42, prestamo.LibroId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Registrar_con_LibroId_cero_o_negativo_lanza_ValidacionDominioException(int libroIdInvalido)
    {
        var lector = CodigoLector.Crear("EST-001");

        Assert.Throws<ValidacionDominioException>(() => Prestamo.Registrar(libroIdInvalido, lector, Inicio, Limite));
    }

    [Theory]
    [InlineData(0)]   // fecha límite igual al inicio
    [InlineData(-1)]  // fecha límite anterior al inicio
    public void Registrar_con_fecha_limite_no_posterior_al_inicio_lanza_ValidacionDominioException(int diasDesdeInicio)
    {
        var lector = CodigoLector.Crear("EST-001");
        var limiteInvalido = Inicio.AddDays(diasDesdeInicio);

        Assert.Throws<ValidacionDominioException>(() => Prestamo.Registrar(1, lector, Inicio, limiteInvalido));
    }

    // ---------- Devolver ----------

    [Fact]
    public void Devolver_un_prestamo_activo_guarda_la_fecha_y_pasa_a_Devuelto()
    {
        var prestamo = RegistrarPrestamoValido();
        var fechaDevolucion = Inicio.AddDays(2);

        prestamo.Devolver(fechaDevolucion);

        Assert.Equal(EstadoPrestamo.Devuelto, prestamo.Estado);
        Assert.Equal(fechaDevolucion, prestamo.FechaDevolucion);
    }

    [Fact]
    public void Un_prestamo_no_permite_devolverse_dos_veces()
    {
        var prestamo = RegistrarPrestamoValido();
        var primeraDevolucion = Inicio.AddDays(2);
        prestamo.Devolver(primeraDevolucion);

        Assert.Throws<ReglaNegocioException>(() => prestamo.Devolver(Inicio.AddDays(3)));
        Assert.Equal(primeraDevolucion, prestamo.FechaDevolucion);
    }

    [Fact]
    public void Devolver_con_fecha_anterior_al_inicio_lanza_ValidacionDominioException_y_el_prestamo_sigue_Activo()
    {
        var prestamo = RegistrarPrestamoValido();

        Assert.Throws<ValidacionDominioException>(() => prestamo.Devolver(Inicio.AddDays(-1)));
        Assert.Equal(EstadoPrestamo.Activo, prestamo.Estado);
        Assert.Null(prestamo.FechaDevolucion);
    }

    // ---------- CodigoLector (Value Object) ----------

    [Theory]
    [InlineData("est-001", "EST-001")]
    [InlineData("  est-001  ", "EST-001")]
    [InlineData("Est-001", "EST-001")]
    [InlineData("EST-001", "EST-001")]
    public void CodigoLector_se_normaliza_con_trim_y_mayusculas(string entrada, string esperado)
    {
        var lector = CodigoLector.Crear(entrada);

        Assert.Equal(esperado, lector.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CodigoLector_vacio_lanza_ValidacionDominioException(string? entrada)
    {
        Assert.Throws<ValidacionDominioException>(() => CodigoLector.Crear(entrada));
    }

    [Fact]
    public void Dos_CodigoLector_con_el_mismo_valor_normalizado_son_iguales()
    {
        Assert.Equal(CodigoLector.Crear(" est-001 "), CodigoLector.Crear("EST-001"));
    }
}
