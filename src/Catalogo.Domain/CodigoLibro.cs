namespace Catalogo.Domain;

/// <summary>
/// Value Object: identifica un libro por su valor. Formato normalizado "LIB-XXXX".
/// </summary>
public sealed record CodigoLibro
{
    private const string Prefijo = "LIB-";

    public string Valor { get; }

    private CodigoLibro(string valor)
    {
        Valor = valor;
    }

    public static CodigoLibro Crear(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ValidacionDominioException("El código del libro es obligatorio.");

        var normalizado = valor.Trim().ToUpperInvariant();

        if (!normalizado.StartsWith(Prefijo, StringComparison.Ordinal))
            normalizado = Prefijo + normalizado;

        if (normalizado.Length == Prefijo.Length)
            throw new ValidacionDominioException($"El código del libro no puede ser solo \"{Prefijo}\".");

        return new CodigoLibro(normalizado);
    }

    public override string ToString() => Valor;
}
