using Grpc.Core;

namespace Biblioteca.GrpcDemo.Services;

/// <summary>
/// Implementación de la demostración gRPC con datos ficticios en memoria.
/// Independiente de Catalogo.Api: no comparte datos ni código con ella.
/// </summary>
public sealed class ConsultasCatalogoService : ConsultasCatalogo.ConsultasCatalogoBase
{
    // Datos ficticios de la demo.
    public static readonly Guid LibroActivo = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid LibroInactivo = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // Solo para demostrar el deadline del cliente: este libro tarda más de 3 s en responder.
    public static readonly Guid LibroLento = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static readonly IReadOnlyDictionary<Guid, (string Titulo, bool Activo)> Libros =
        new Dictionary<Guid, (string Titulo, bool Activo)>
        {
            [LibroActivo] = ("Domain-Driven Design", true),
            [LibroInactivo] = ("Clean Code", false),
            [LibroLento] = ("Patterns of Enterprise Application Architecture", true),
        };

    private readonly ILogger<ConsultasCatalogoService> _logger;

    public ConsultasCatalogoService(ILogger<ConsultasCatalogoService> logger)
    {
        _logger = logger;
    }

    public override async Task<LibroReply> ConsultarLibro(LibroRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "El id del libro debe ser un Guid válido."));

        if (id == LibroLento)
        {
            _logger.LogInformation("Simulando una respuesta lenta para el libro {LibroId}.", id);
            // Respeta la cancelación: si el cliente vence su deadline, se deja de esperar.
            await Task.Delay(TimeSpan.FromSeconds(5), context.CancellationToken);
        }

        if (!Libros.TryGetValue(id, out var libro))
            return new LibroReply { Existe = false };

        return new LibroReply { Existe = true, Activo = libro.Activo, Titulo = libro.Titulo };
    }
}
