namespace Catalogo.Api.Mensajeria.Options;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ) · <b>Tipo:</b> Opciones de configuración (sección "RabbitMq" de appsettings)</para>
/// Copia propia de Catálogo (no comparte código con Préstamos). Las credenciales no van en appsettings.json:
/// en desarrollo vienen de appsettings.Development.json (guest/guest, solo válido desde localhost)
/// y en otros entornos de variables de entorno (RabbitMq__UserName, RabbitMq__Password).
/// </summary>
public sealed class RabbitMqOptions
{
    public const string Seccion = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string? UserName { get; set; }
    public string? Password { get; set; }

    /// <summary>Exchange topic donde Préstamos publica sus eventos.</summary>
    public string Exchange { get; set; } = "biblioteca.prestamos";

    /// <summary>Cola durable de la que Catálogo consume prestamo.registrado.</summary>
    public string Cola { get; set; } = "catalogo.prestamos.registrados";

    /// <summary>Mensajes entregados sin confirmar como máximo (BasicQos).</summary>
    public ushort Prefetch { get; set; } = 10;

    /// <summary>
    /// SOLO PARA PRUEBAS. Cantidad de entregas en las que el consumidor simula una caída
    /// después de procesar el mensaje y antes del ACK (el mensaje se reentrega como Redelivered).
    /// 0 = desactivado (valor por defecto).
    /// </summary>
    public int CaidasAntesDeAck { get; set; }
}
