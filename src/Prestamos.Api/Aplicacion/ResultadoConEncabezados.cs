namespace Prestamos.Api.Aplicacion;

/// <summary>
/// Envuelve un IResult y agrega encabezados HTTP a la respuesta antes de ejecutarlo.
/// Expone el código de estado y el valor del resultado interno para poder inspeccionarlos en pruebas.
/// </summary>
public sealed class ResultadoConEncabezados : IResult, IStatusCodeHttpResult, IValueHttpResult
{
    private readonly IResult _interno;

    public ResultadoConEncabezados(IResult interno, IReadOnlyDictionary<string, string> encabezados)
    {
        _interno = interno;
        Encabezados = encabezados;
    }

    public IReadOnlyDictionary<string, string> Encabezados { get; }

    public int? StatusCode => (_interno as IStatusCodeHttpResult)?.StatusCode;

    public object? Value => (_interno as IValueHttpResult)?.Value;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        foreach (var (nombre, valor) in Encabezados)
            httpContext.Response.Headers[nombre] = valor;

        return _interno.ExecuteAsync(httpContext);
    }
}
