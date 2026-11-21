using Biblioteca.GrpcDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// gRPC sobre HTTP/2: Kestrel:EndpointDefaults:Protocols = Http2 en appsettings.json.
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<ConsultasCatalogoService>();
app.MapGet("/", () => "Demo gRPC de Biblioteca: usa un cliente gRPC (ConsultasCatalogo/ConsultarLibro).");

app.Run();
