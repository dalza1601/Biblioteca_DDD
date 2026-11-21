using Prestamos.Api.Integrations;

namespace Biblioteca.Tests.Integracion;

/// <summary>
/// Fake de ILibroConsultaClient: no hace HTTP. Devuelve un libro fijo (o null) o lanza una excepción,
/// y registra cada consulta recibida para poder verificar si el caso de uso llamó o no a Catálogo.
/// </summary>
internal sealed class LibroConsultaClientFake : ILibroConsultaClient
{
    private readonly LibroConsultaDto? _libro;
    private readonly Exception? _excepcion;

    private LibroConsultaClientFake(LibroConsultaDto? libro, Exception? excepcion)
    {
        _libro = libro;
        _excepcion = excepcion;
    }

    public List<int> Consultas { get; } = new();

    public static LibroConsultaClientFake QueDevuelve(LibroConsultaDto? libro) => new(libro, null);

    public static LibroConsultaClientFake QueLanza(Exception excepcion) => new(null, excepcion);

    public Task<LibroConsultaDto?> ObtenerLibroAsync(int id, CancellationToken ct)
    {
        Consultas.Add(id);

        return _excepcion is null
            ? Task.FromResult(_libro)
            : Task.FromException<LibroConsultaDto?>(_excepcion);
    }
}
