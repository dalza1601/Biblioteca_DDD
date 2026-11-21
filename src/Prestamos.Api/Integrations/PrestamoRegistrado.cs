using System.Text.Json;

namespace Prestamos.Api.Integrations;

/// <summary>
/// Evento de integración que Préstamos publica cuando registra un préstamo.
/// Es un contrato público entre servicios: inmutable, versionado y sin datos personales
/// (no incluye el lector) ni tipos de Prestamos.Domain.
/// </summary>
public sealed record PrestamoRegistrado(
    Guid EventoId,
    int PrestamoId,
    int LibroId,
    DateTime FechaRegistroUtc,
    int Version = 1)
{
    /// <summary>Routing key con la que se publica en RabbitMQ.</summary>
    public const string RoutingKey = "prestamo.registrado";

    /// <summary>Opciones JSON del evento: camelCase (JsonSerializerDefaults.Web).</summary>
    public static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    /// <summary>Crea el evento con un EventoId nuevo, la hora actual UTC y versión 1.</summary>
    public static PrestamoRegistrado Crear(int prestamoId, int libroId) =>
        new(Guid.NewGuid(), prestamoId, libroId, DateTime.UtcNow, Version: 1);

    /// <summary>Serializa el evento a JSON UTF-8 en camelCase, listo para el cuerpo del mensaje.</summary>
    public byte[] Serializar() => JsonSerializer.SerializeToUtf8Bytes(this, OpcionesJson);
}
