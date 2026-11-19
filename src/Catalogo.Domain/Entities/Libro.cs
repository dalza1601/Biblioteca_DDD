using Catalogo.Domain.Exceptions;
using Catalogo.Domain.ValueObjects;

namespace Catalogo.Domain.Entities;

/// <summary>
/// Entidad: se identifica por su Id y protege sus propias reglas (título válido, desactivación).
/// </summary>
public sealed class Libro
{
    // TEMPORAL: mientras no exista persistencia, el Id se asigna con un contador en memoria (1, 2, 3...).
    // Al implementar la base de datos, el Id lo generará la persistencia y este contador debe eliminarse.
    private static int _ultimoId;

    public int Id { get; private set; }
    public CodigoLibro Codigo { get; private set; }
    public string Titulo { get; private set; }
    public bool Activo { get; private set; }

    private Libro(CodigoLibro codigo, string titulo)
    {
        Id = Interlocked.Increment(ref _ultimoId);
        Codigo = codigo;
        Titulo = titulo;
        Activo = true;
    }

    public static Libro Crear(CodigoLibro codigo, string? titulo)
    {
        if (codigo is null)
            throw new ValidacionDominioException("El código del libro es obligatorio.");

        return new Libro(codigo, ValidarTitulo(titulo));
    }

    public void CambiarTitulo(string? nuevoTitulo)
    {
        Titulo = ValidarTitulo(nuevoTitulo);
    }

    public void Desactivar()
    {
        if (!Activo)
            throw new ReglaNegocioException("El libro ya está inactivo.");

        Activo = false;
    }

    public void Activar()
    {
        if (Activo)
            throw new ReglaNegocioException("El libro ya está activo.");

        Activo = true;
    }

    private static string ValidarTitulo(string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ValidacionDominioException("El título del libro es obligatorio.");

        return titulo.Trim();
    }
}
