using Catalogo.Domain.Entities;
using Catalogo.Domain.Exceptions;
using Catalogo.Domain.ValueObjects;

namespace Biblioteca.Tests.Catalogo;

public class LibroActivacionTests
{
    private static Libro CrearLibroValido() =>
        Libro.Crear(CodigoLibro.Crear("ABC-001"), "Domain-Driven Design");

    [Fact]
    public void Activar_un_libro_inactivo_lo_reactiva()
    {
        var libro = CrearLibroValido();
        libro.Desactivar();

        libro.Activar();

        Assert.True(libro.Activo);
    }

    [Fact]
    public void Activar_un_libro_ya_activo_lanza_ReglaNegocioException()
    {
        var libro = CrearLibroValido();

        Assert.Throws<ReglaNegocioException>(() => libro.Activar());
        Assert.True(libro.Activo);
    }
}
