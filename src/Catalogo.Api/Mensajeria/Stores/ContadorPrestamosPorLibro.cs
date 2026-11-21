using System.Collections.Concurrent;

namespace Catalogo.Api.Mensajeria.Stores;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ, consumo de prestamo.registrado) · <b>Tipo:</b> Almacén en memoria (métrica, Singleton)</para>
/// Cuenta préstamos por libro a partir de los eventos recibidos. Solo en memoria: se pierde al reiniciar.
/// No es idempotente por sí mismo: la idempotencia (no contar dos veces el mismo EventoId)
/// la garantiza ProcesadorPrestamoRegistrado. No toca la entidad Libro ni su repositorio.
/// </summary>
public sealed class ContadorPrestamosPorLibro
{
    private readonly ConcurrentDictionary<int, int> _prestamosPorLibro = new();

    /// <summary>Suma un préstamo al libro.</summary>
    public void Incrementar(int libroId) =>
        _prestamosPorLibro.AddOrUpdate(libroId, 1, (_, actual) => actual + 1);

    /// <summary>Copia del estado actual ordenada por LibroId.</summary>
    public IReadOnlyList<(int LibroId, int Prestamos)> Instantanea() =>
        _prestamosPorLibro
            .OrderBy(par => par.Key)
            .Select(par => (par.Key, par.Value))
            .ToList();
}
