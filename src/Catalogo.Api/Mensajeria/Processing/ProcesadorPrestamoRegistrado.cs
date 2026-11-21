using Catalogo.Api.Mensajeria.Contracts;
using Catalogo.Api.Mensajeria.Stores;

namespace Catalogo.Api.Mensajeria.Processing;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ, consumo de prestamo.registrado) · <b>Tipo:</b> Procesador de mensajes (lógica sin RabbitMQ, Singleton)</para>
/// Recibe los bytes del mensaje y decide:
///   - inválido  → lo guarda en cuarentena con su motivo
///   - duplicado → el EventoId ya se contó; no hace nada
///   - procesado → suma un préstamo al libro en el contador
/// No conoce RabbitMQ (se puede probar con bytes) y no modifica la entidad Libro.
/// </summary>
public sealed class ProcesadorPrestamoRegistrado
{
    private readonly ContadorPrestamosPorLibro _contador;
    private readonly Cuarentena _cuarentena;
    private readonly ILogger<ProcesadorPrestamoRegistrado> _logger;

    public ProcesadorPrestamoRegistrado(
        ContadorPrestamosPorLibro contador,
        Cuarentena cuarentena,
        ILogger<ProcesadorPrestamoRegistrado> logger)
    {
        _contador = contador;
        _cuarentena = cuarentena;
        _logger = logger;
    }

    public ResultadoProcesamiento Procesar(byte[] cuerpo)
    {
        if (!PrestamoRegistradoMensaje.TryLeer(cuerpo, out var mensaje, out var motivo))
        {
            _cuarentena.Agregar(motivo, cuerpo);
            _logger.LogWarning("Mensaje prestamo.registrado inválido enviado a cuarentena: {Motivo}", motivo);
            return ResultadoProcesamiento.Invalido;
        }

        if (!_contador.Registrar(mensaje.EventoId, mensaje.LibroId))
        {
            _logger.LogInformation("Evento {EventoId} duplicado: ya se había contado.", mensaje.EventoId);
            return ResultadoProcesamiento.Duplicado;
        }

        _logger.LogInformation("Préstamo {PrestamoId} del libro {LibroId} contado (evento {EventoId}).",
            mensaje.PrestamoId, mensaje.LibroId, mensaje.EventoId);
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
