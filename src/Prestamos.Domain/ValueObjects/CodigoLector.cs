using Prestamos.Domain.Exceptions;

namespace Prestamos.Domain.ValueObjects;

/// <summary>
/// Value Object: identifica al estudiante lector por su valor normalizado (sin espacios, en mayúsculas).
/// </summary>
public sealed record CodigoLector
{
    public string Valor { get; }

    private CodigoLector(string valor)
    {
        Valor = valor;
    }

    public static CodigoLector Crear(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ValidacionDominioException("El código del lector es obligatorio.");

        return new CodigoLector(valor.Trim().ToUpperInvariant());
    }

    public override string ToString() => Valor;
}
