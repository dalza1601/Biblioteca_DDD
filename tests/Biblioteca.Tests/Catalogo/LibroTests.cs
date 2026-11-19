using Catalogo.Domain.Entities;
using Catalogo.Domain.Exceptions;
using Catalogo.Domain.ValueObjects;

namespace Biblioteca.Tests.Catalogo;

public class LibroTests
{
    private static Libro CrearLibroValido() =>
        Libro.Crear(CodigoLibro.Crear("ABC-001"), "Domain-Driven Design");

    // ---------- Libro ----------

    [Fact]
    public void Un_libro_recien_creado_esta_activo()
    {
        var libro = CrearLibroValido();

        Assert.True(libro.Activo);
    }

    [Fact]
    public void Un_libro_recien_creado_guarda_el_titulo_sin_espacios_extremos()
    {
        var libro = Libro.Crear(CodigoLibro.Crear("ABC-001"), "  Clean Code  ");

        Assert.Equal("Clean Code", libro.Titulo);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Crear_un_libro_con_titulo_vacio_lanza_ValidacionDominioException(string? tituloInvalido)
    {
        var codigo = CodigoLibro.Crear("ABC-001");

        Assert.Throws<ValidacionDominioException>(() => Libro.Crear(codigo, tituloInvalido));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Cambiar_el_titulo_a_vacio_lanza_ValidacionDominioException_y_conserva_el_anterior(string? tituloInvalido)
    {
        var libro = CrearLibroValido();

        Assert.Throws<ValidacionDominioException>(() => libro.CambiarTitulo(tituloInvalido));
        Assert.Equal("Domain-Driven Design", libro.Titulo);
    }

    [Fact]
    public void Desactivar_un_libro_activo_lo_deja_inactivo()
    {
        var libro = CrearLibroValido();

        libro.Desactivar();

        Assert.False(libro.Activo);
    }

    [Fact]
    public void Desactivar_un_libro_dos_veces_lanza_ReglaNegocioException()
    {
        var libro = CrearLibroValido();
        libro.Desactivar();

        Assert.Throws<ReglaNegocioException>(() => libro.Desactivar());
        Assert.False(libro.Activo);
    }

    // ---------- CodigoLibro (Value Object) ----------

    [Theory]
    [InlineData("abc1", "LIB-ABC1")]
    [InlineData("  abc1  ", "LIB-ABC1")]
    [InlineData("lib-abc1", "LIB-ABC1")]
    [InlineData("LIB-ABC1", "LIB-ABC1")]
    [InlineData("  Lib-Abc1 ", "LIB-ABC1")]
    public void CodigoLibro_se_normaliza_con_trim_mayusculas_y_prefijo_sin_duplicarlo(string entrada, string esperado)
    {
        var codigo = CodigoLibro.Crear(entrada);

        Assert.Equal(esperado, codigo.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CodigoLibro_vacio_lanza_ValidacionDominioException(string? entrada)
    {
        Assert.Throws<ValidacionDominioException>(() => CodigoLibro.Crear(entrada));
    }

    [Theory]
    [InlineData("LIB-")]
    [InlineData("lib-")]
    [InlineData("  LIB-  ")]
    public void CodigoLibro_con_solo_el_prefijo_lanza_ValidacionDominioException(string entrada)
    {
        Assert.Throws<ValidacionDominioException>(() => CodigoLibro.Crear(entrada));
    }

    [Fact]
    public void Dos_CodigoLibro_con_el_mismo_valor_normalizado_son_iguales()
    {
        var codigo1 = CodigoLibro.Crear("abc1");
        var codigo2 = CodigoLibro.Crear("  LIB-ABC1 ");

        Assert.Equal(codigo1, codigo2);
        Assert.True(codigo1 == codigo2);
        Assert.Equal(codigo1.GetHashCode(), codigo2.GetHashCode());
        Assert.NotSame(codigo1, codigo2);
    }

    [Fact]
    public void Dos_CodigoLibro_con_valores_distintos_no_son_iguales()
    {
        var codigo1 = CodigoLibro.Crear("abc1");
        var codigo2 = CodigoLibro.Crear("abc2");

        Assert.NotEqual(codigo1, codigo2);
        Assert.True(codigo1 != codigo2);
    }
}
