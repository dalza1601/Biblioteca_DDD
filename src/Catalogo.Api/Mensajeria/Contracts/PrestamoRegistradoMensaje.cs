using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Catalogo.Api.Mensajeria.Contracts;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ, consumo de prestamo.registrado) · <b>Tipo:</b> Contrato del mensaje entrante (copia propia)</para>
/// Copia del contrato que publica Préstamos. Catálogo no referencia a Préstamos: si el productor
/// cambia el formato, la validación de TryLeer lo detecta y el mensaje va a cuarentena.
/// </summary>
public sealed record PrestamoRegistradoMensaje(
    Guid EventoId,
    int PrestamoId,
    int LibroId,
    DateTime FechaRegistroUtc,
    int Version)
{
    public const string RoutingKey = "prestamo.registrado";
    public const int VersionSoportada = 1;

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Lee y valida el cuerpo del mensaje. Devuelve false y un motivo legible si es inválido:
    /// JSON mal formado, eventoId vacío, prestamoId o libroId vacíos (≤ 0),
    /// fechaRegistroUtc ausente o versión distinta de 1.
    /// </summary>
    public static bool TryLeer(
        byte[] cuerpo,
        [NotNullWhen(true)] out PrestamoRegistradoMensaje? mensaje,
        [NotNullWhen(false)] out string? motivo)
    {
        mensaje = null;

        Crudo? crudo;
        try
        {
            crudo = JsonSerializer.Deserialize<Crudo>(cuerpo, OpcionesJson);
        }
        catch (JsonException)
        {
            motivo = "El cuerpo no es un JSON válido o tiene tipos incorrectos.";
            return false;
        }

        motivo = Validar(crudo);
        if (motivo is not null)
            return false;

        mensaje = new PrestamoRegistradoMensaje(
            crudo!.EventoId!.Value,
            crudo.PrestamoId!.Value,
            crudo.LibroId!.Value,
            crudo.FechaRegistroUtc!.Value.ToUniversalTime(),
            crudo.Version!.Value);
        return true;
    }

    private static string? Validar(Crudo? crudo)
    {
        if (crudo is null)
            return "El cuerpo está vacío.";
        if (crudo.EventoId is null || crudo.EventoId == Guid.Empty)
            return "eventoId está vacío.";
        if (crudo.PrestamoId is null or <= 0)
            return "prestamoId está vacío o no es positivo.";
        if (crudo.LibroId is null or <= 0)
            return "libroId está vacío o no es positivo.";
        if (crudo.FechaRegistroUtc is null || crudo.FechaRegistroUtc == default(DateTime))
            return "Falta fechaRegistroUtc.";
        if (crudo.Version is null)
            return "Falta version.";
        if (crudo.Version != VersionSoportada)
            return $"Versión {crudo.Version} no soportada (se espera {VersionSoportada}).";

        return null;
    }

    // Todos los campos opcionales para poder distinguir "ausente" de "valor por defecto".
    private sealed record Crudo(Guid? EventoId, int? PrestamoId, int? LibroId, DateTime? FechaRegistroUtc, int? Version);
}
