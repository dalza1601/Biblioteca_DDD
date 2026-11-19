using Prestamos.Domain.Entities;
using Prestamos.Domain.ValueObjects;

namespace Biblioteca.Tests.Prestamos;

public class PrestamoVencimientoTests
{
    private static readonly DateOnly Inicio = new(2026, 11, 18);
    private static readonly DateOnly Limite = new(2026, 11, 25);

    private static Prestamo RegistrarPrestamoValido() =>
        Prestamo.Registrar(1, CodigoLector.Crear("EST-001"), Inicio, Limite);

    [Fact]
    public void Un_prestamo_activo_esta_vencido_el_dia_despues_del_limite()
    {
        var prestamo = RegistrarPrestamoValido();

        Assert.True(prestamo.EstaVencido(Limite.AddDays(1)));
    }

    [Fact]
    public void Un_prestamo_activo_no_esta_vencido_el_mismo_dia_del_limite()
    {
        var prestamo = RegistrarPrestamoValido();

        Assert.False(prestamo.EstaVencido(Limite));
    }

    [Fact]
    public void Un_prestamo_devuelto_no_esta_vencido_aunque_haya_pasado_el_limite()
    {
        var prestamo = RegistrarPrestamoValido();
        prestamo.Devolver(Inicio.AddDays(2));

        Assert.False(prestamo.EstaVencido(Limite.AddDays(10)));
    }
}
