using Catalogo.Api.Mensajeria.Contracts;
using Catalogo.Api.Mensajeria.Stores;

namespace Catalogo.Api.Mensajeria.Processing;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ, consumo de prestamo.registrado) · <b>Tipo:</b> Procesador de mensajes idempotente (lógica sin RabbitMQ, Singleton)</para>
/// Recibe los bytes del mensaje y decide:
///   - inválido  → lo guarda en cuarentena con su motivo
///   - duplicado → el EventoId ya se procesó; NO toca el contador
///   - procesado → suma un préstamo al libro y registra el EventoId
///
/// Idempotencia: un HashSet&lt;Guid&gt; en memoria con los EventoId procesados. Consultar el set,
/// aplicar el efecto sobre el contador y registrar el EventoId ocurren dentro de un mismo lock,
/// como una sola unidad: dos entregas simultáneas del mismo evento no pueden contar dos veces.
/// Así, si el consumidor falla después de procesar y antes del ACK, la reentrega es inofensiva.
/// No conoce RabbitMQ (se puede probar con bytes) y no modifica la entidad Libro.
/// El set vive en memoria: se pierde al reiniciar la API.
/// </summary>
public sealed class ProcesadorPrestamoRegistrado
{
    private readonly ContadorPrestamosPorLibro _contador;
    private readonly Cuarentena _cuarentena;
    private readonly ILogger<ProcesadorPrestamoRegistrado> _logger;

    private readonly HashSet<Guid> _eventosProcesados = new();
    private readonly Lock _lock = new();

    public ProcesadorPrestamoRegistrado(
        ContadorPrestamosPorLibro contador,
        Cuarentena cuarentena,
        ILogger<ProcesadorPrestamoRegistrado> logger)
    {
        _contador = contador;
        _cuarentena = cuarentena;
        _logger = logger;
    }

    /// <param name="cuerpo">Bytes del mensaje.</param>
    /// <param name="reentregado">true si RabbitMQ marcó la entrega como Redelivered.</param>
    public ResultadoProcesamiento Procesar(byte[] cuerpo, bool reentregado = false)
    {
        if (!PrestamoRegistradoMensaje.TryLeer(cuerpo, out var mensaje, out var motivo))
        {
            _cuarentena.Agregar(motivo, cuerpo);
            _logger.LogWarning("Mensaje prestamo.registrado inválido enviado a cuarentena: {Motivo}", motivo);
            return ResultadoProcesamiento.Invalido;
        }

        bool duplicado;
        lock (_lock)
        {
            // Consulta + efecto + registro como una sola unidad.
            duplicado = _eventosProcesados.Contains(mensaje.EventoId);
            if (!duplicado)
            {
                _contador.Incrementar(mensaje.LibroId);
                _eventosProcesados.Add(mensaje.EventoId);
            }
        }

        if (duplicado)
        {
            if (reentregado)
                _logger.LogWarning(
                    "Evento {EventoId} llegó como REDELIVERED y ya estaba procesado: se descarta sin tocar el contador (idempotencia).",
                    mensaje.EventoId);
            else
                _logger.LogInformation("Evento {EventoId} duplicado: ya estaba procesado; no se toca el contador.",
                    mensaje.EventoId);

            return ResultadoProcesamiento.Duplicado;
        }

        _logger.LogInformation("Préstamo {PrestamoId} del libro {LibroId} contado (evento {EventoId}, redelivered = {Reentregado}).",
            mensaje.PrestamoId, mensaje.LibroId, mensaje.EventoId, reentregado);
        return ResultadoProcesamiento.Procesado;
    }
}

/// <summary>Resultado de procesar un mensaje prestamo.registrado.</summary>
public enum ResultadoProcesamiento
{
    Procesado,
    Duplicado,
    Invalido
}
