using Biblioteca.GrpcDemo;
using Grpc.Core;
using Grpc.Net.Client;

// Uso:  dotnet run [direccion] [id1 id2 ...]
// Por defecto llama al servidor de la demo por HTTPS (HTTP/2) y consulta un conjunto de ids de ejemplo.
var direccion = args.Length > 0 ? args[0] : "https://localhost:7135";
string[] ids = args.Length > 1
    ? args[1..]
    :
    [
        "11111111-1111-1111-1111-111111111111", // existe y activo
        "22222222-2222-2222-2222-222222222222", // existe e inactivo
        "33333333-3333-3333-3333-333333333333", // no existe
        "no-es-un-guid",                        // InvalidArgument
        "99999999-9999-9999-9999-999999999999", // lento: supera el deadline de 3 s
    ];

using var canal = GrpcChannel.ForAddress(direccion);
var cliente = new ConsultasCatalogo.ConsultasCatalogoClient(canal);

Console.WriteLine($"Servidor: {direccion}");
foreach (var id in ids)
    Console.WriteLine($"  {id,-38} -> {await ConsultarAsync(cliente, id)}");

static async Task<string> ConsultarAsync(ConsultasCatalogo.ConsultasCatalogoClient cliente, string id)
{
    try
    {
        var respuesta = await cliente.ConsultarLibroAsync(
            new LibroRequest { Id = id },
            deadline: DateTime.UtcNow.AddSeconds(3));

        if (!respuesta.Existe)
            return "no existe";

        return respuesta.Activo
            ? $"existe y está activo: \"{respuesta.Titulo}\""
            : $"existe pero está inactivo: \"{respuesta.Titulo}\"";
    }
    catch (RpcException ex) when (ex.StatusCode is StatusCode.DeadlineExceeded or StatusCode.Unavailable)
    {
        // Sin respuesta no se puede afirmar que el libro exista o no exista.
        return $"no sé ({ex.StatusCode}): Catálogo no respondió a tiempo o no está disponible";
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
    {
        return $"petición inválida ({ex.StatusCode}): {ex.Status.Detail}";
    }
    catch (RpcException ex)
    {
        // Cualquier otro fallo (p. ej. certificado TLS no confiable) se informa sin stack trace.
        return $"error gRPC ({ex.StatusCode}): {ex.Status.Detail}";
    }
}
