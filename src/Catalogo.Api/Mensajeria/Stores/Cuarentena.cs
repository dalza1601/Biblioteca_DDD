using System.Collections.Concurrent;
using System.Text;

namespace Catalogo.Api.Mensajeria.Stores;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ, consumo de prestamo.registrado) · <b>Tipo:</b> Almacén en memoria de mensajes inválidos (Singleton)</para>
/// Guarda los mensajes que no pasaron la validación, con su motivo, para poder revisarlos.
/// Solo en memoria y acotada a los últimos <see cref="Capacidad"/> mensajes para no crecer sin límite.
/// </summary>
public sealed class Cuarentena
{
    public const int Capacidad = 100;
    private const int MaxCaracteresContenido = 1000;

    private readonly ConcurrentQueue<MensajeEnCuarentena> _mensajes = new();

    public void Agregar(string motivo, byte[] cuerpo)
    {
        var contenido = Encoding.UTF8.GetString(cuerpo);
        if (contenido.Length > MaxCaracteresContenido)
            contenido = contenido[..MaxCaracteresContenido] + "…";

        _mensajes.Enqueue(new MensajeEnCuarentena(DateTime.UtcNow, motivo, contenido));

        while (_mensajes.Count > Capacidad && _mensajes.TryDequeue(out _)) { }
    }

    public IReadOnlyList<MensajeEnCuarentena> Listar() => _mensajes.ToArray();
}

/// <summary>Mensaje inválido retenido en cuarentena.</summary>
public sealed record MensajeEnCuarentena(DateTime FechaUtc, string Motivo, string Contenido);
