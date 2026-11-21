using System.Collections.Concurrent;

namespace Catalogo.Api.Mensajeria.Stores;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ, consumo de prestamo.registrado) · <b>Tipo:</b> Almacén en memoria (métrica, Singleton)</para>
/// Cuenta préstamos por libro a partir de los eventos recibidos. Solo en memoria: se pierde al reiniciar.
/// Es idempotente: recuerda los EventoId ya contados para que un mensaje repetido no sume dos veces.
/// No toca la entidad Libro ni su repositorio.
/// </summary>
public sealed class ContadorPrestamosPorLibro
{
    private readonly ConcurrentDictionary<int, int> _prestamosPorLibro = new();
    private readonly ConcurrentDictionary<Guid, byte> _eventosContados = new();

    /// <summary>Suma un préstamo al libro. Devuelve false si el evento ya se había contado (duplicado).</summary>
    public bool Registrar(Guid eventoId, int libroId)
    {
        if (!_eventosContados.TryAdd(eventoId, 0))
            return false;

        _prestamosPorLibro.AddOrUpdate(libroId, 1, (_, actual) => actual + 1);
        return true;
    }

    public int EventosContados => _eventosContados.Count;

    /// <summary>Copia del estado actual ordenada por LibroId.</summary>
    public IReadOnlyList<(int LibroId, int Prestamos)> Instantanea() =>
        _prestamosPorLibro
            .OrderBy(par => par.Key)
            .Select(par => (par.Key, par.Value))
            .ToList();
}
