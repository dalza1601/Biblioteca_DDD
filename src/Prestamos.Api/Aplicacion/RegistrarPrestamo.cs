using Prestamos.Api.Integrations.Catalogo.Clients;
using Prestamos.Api.Integrations.Catalogo.Contracts;
using Prestamos.Api.Integrations.Catalogo.Exceptions;
using Prestamos.Api.Integrations.Mensajeria.Events;
using Prestamos.Api.Integrations.Mensajeria.Publishers;
using Prestamos.Application.Contracts;
using Prestamos.Application.IRepository;
using Prestamos.Application.Mappers;
using Prestamos.Domain.Entities;
using Prestamos.Domain.Exceptions;
using Prestamos.Domain.ValueObjects;

namespace Prestamos.Api.Aplicacion;

/// <summary>
/// Caso de uso "registrar préstamo":
///   1. Validaciones baratas, sin red (CodigoLector primero y campos obligatorios).
///   2. Consulta el libro a Catálogo y decide:
///        200 activo   → continúa (Creado)
///        200 inactivo → LibroNoDisponible
///        null (404)   → LibroNoExiste
///        CatalogoNoDisponibleException → CatalogoNoDisponible
///        CatalogoTimeoutException      → CatalogoTimeout
///   3. Recién entonces aplica las reglas del dominio con Prestamo.Registrar() y guarda.
///   4. Solo después de guardar, publica PrestamoRegistrado (timeout de 4 s). Si la publicación
///      falla, el préstamo NO se revierte: se registra el error y se informa EventoPublicado = false.
/// </summary>
public sealed class RegistrarPrestamo
{
    public static readonly TimeSpan TimeoutPublicacion = TimeSpan.FromSeconds(4);

    private readonly ILibroConsultaClient _catalogo;
    private readonly IRepositorioPrestamos _repositorio;
    private readonly IEventPublisher _publicador;
    private readonly ILogger<RegistrarPrestamo> _logger;

    public RegistrarPrestamo(
        ILibroConsultaClient catalogo,
        IRepositorioPrestamos repositorio,
        IEventPublisher publicador,
        ILogger<RegistrarPrestamo> logger)
    {
        _catalogo = catalogo;
        _repositorio = repositorio;
        _publicador = publicador;
        _logger = logger;
    }

    public async Task<ResultadoRegistrarPrestamo> EjecutarAsync(RegistrarPrestamoComando comando, CancellationToken ct)
    {
        // 1. Validaciones baratas: si fallan, no vale la pena llamar a Catálogo (→ 400 en el middleware).
        var lector = CodigoLector.Crear(comando.Lector);

        if (comando.LibroId is not { } libroId || libroId <= 0)
            throw new ValidacionDominioException("El identificador del libro debe ser mayor que cero.");

        var fechaInicio = comando.FechaInicio
            ?? throw new ValidacionDominioException("La fecha de inicio es obligatoria.");
        var fechaLimite = comando.FechaLimite
            ?? throw new ValidacionDominioException("La fecha límite es obligatoria.");

        // 2. Consulta a Catálogo y decisión según la tabla. En los casos sin préstamo no se publica nada.
        LibroConsultaDto? libro;
        try
        {
            libro = await _catalogo.ObtenerLibroAsync(libroId, ct);
        }
        catch (CatalogoTimeoutException)
        {
            return new ResultadoRegistrarPrestamo.CatalogoTimeout();
        }
        catch (CatalogoNoDisponibleException)
        {
            return new ResultadoRegistrarPrestamo.CatalogoNoDisponible();
        }

        if (libro is null)
            return new ResultadoRegistrarPrestamo.LibroNoExiste(libroId);

        if (!libro.Activo)
            return new ResultadoRegistrarPrestamo.LibroNoDisponible(libroId);

        // 3. Reglas del dominio (p. ej. fecha límite posterior al inicio) y persistencia.
        var prestamo = Prestamo.Registrar(libroId, lector, fechaInicio, fechaLimite);
        _repositorio.Agregar(prestamo);

        // 4. Evento de integración, solo después de guardar.
        var evento = PrestamoRegistrado.Crear(prestamo.Id, prestamo.LibroId);
        var publicado = await PublicarAsync(evento, ct);

        return new ResultadoRegistrarPrestamo.Creado(prestamo.ADto(), evento.EventoId, publicado);
    }

    private async Task<bool> PublicarAsync(PrestamoRegistrado evento, CancellationToken ct)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TimeoutPublicacion);

        try
        {
            await _publicador.PublicarAsync(PrestamoRegistrado.RoutingKey, evento, limite.Token);
            return true;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Incluye el timeout de 4 s. El préstamo ya está guardado y no se revierte.
            _logger.LogError(ex,
                "No se pudo publicar {Evento} {EventoId} del préstamo {PrestamoId}; el préstamo se conserva.",
                nameof(PrestamoRegistrado), evento.EventoId, evento.PrestamoId);
            return false;
        }
    }
}
