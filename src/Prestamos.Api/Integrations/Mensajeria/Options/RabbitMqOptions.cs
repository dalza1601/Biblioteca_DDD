namespace Prestamos.Api.Integrations.Mensajeria.Options;

/// <summary>
/// <para><b>Integración:</b> Mensajería (RabbitMQ) · <b>Tipo:</b> Opciones de configuración (sección "RabbitMq" de appsettings)</para>
/// Datos de conexión y topología. Las credenciales no van en appsettings.json:
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

    /// <summary>Exchange de tipo topic donde se publican los eventos de Préstamos.</summary>
    public string Exchange { get; set; } = "biblioteca.prestamos";

    /// <summary>Cola durable que recibe los eventos prestamo.registrado.</summary>
    public string Cola { get; set; } = "catalogo.prestamos.registrados";
}
