using Prestamos.Api.Aplicacion;
using Prestamos.Api.Endpoints;
using Prestamos.Application.IClients;
using Prestamos.Infrastructure.Clients;
using Prestamos.Infrastructure.Handlers;
using Prestamos.Api.Integrations.Mensajeria.Options;
using Prestamos.Api.Integrations.Mensajeria.Publishers;
using Prestamos.Api.Middleware;
using Prestamos.Application.IRepository;
using Prestamos.Application.Services;
using Prestamos.Infrastructure.Repository;

// ---------- Composition root ----------
var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Los errores de lectura del body (JSON inválido, tipos incorrectos) se lanzan como excepción
// también fuera de Development, para que los traduzca el middleware.
builder.Services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = true);

// Infrastructure: Singleton para conservar los datos en memoria entre peticiones.
builder.Services.AddSingleton<IRepositorioPrestamos, PrestamoRepositoryEnMemoria>();
// Application: un servicio por petición.
builder.Services.AddScoped<ServicioPrestamos>();
// Caso de uso que consulta a Catálogo antes de registrar un préstamo.
builder.Services.AddScoped<RegistrarPrestamo>();

// Integración con Catálogo: puerto ILibroCatalogoClient (Application) implementado por
// CatalogoHttpClient (Infrastructure), un cliente tipado gestionado por IHttpClientFactory (sin new HttpClient()).
// La configuración se lee al arrancar para fallar pronto si falta.
var catalogoApi = builder.Configuration.GetSection("CatalogoApi");
var catalogoBaseUrl = catalogoApi["BaseUrl"]
    ?? throw new InvalidOperationException("Falta la configuración 'CatalogoApi:BaseUrl' en appsettings.json.");
// Con barra final, la ruta relativa "libros/{id}" se combina bien aunque la URL incluya un prefijo (p. ej. un gateway).
if (!catalogoBaseUrl.EndsWith('/'))
    catalogoBaseUrl += "/";
var catalogoTimeout = TimeSpan.FromSeconds(catalogoApi.GetValue("TimeoutSeconds", 3));
var catalogoReintentos = catalogoApi.GetValue("Reintentos", 1); // intentos totales; 1 = sin reintentos

var clienteCatalogo = builder.Services.AddHttpClient<ILibroCatalogoClient, CatalogoHttpClient>(cliente =>
{
    cliente.BaseAddress = new Uri(catalogoBaseUrl);
    cliente.Timeout = catalogoTimeout;
});

// Reintentos de GET: apagados por defecto; solo se activan si CatalogoApi:Reintentos > 1.
if (catalogoReintentos > 1)
{
    builder.Services.AddTransient(sp => new ReintentoGetHandler(
        catalogoReintentos, sp.GetRequiredService<ILogger<ReintentoGetHandler>>()));
    clienteCatalogo.AddHttpMessageHandler<ReintentoGetHandler>();
}

// Integración con RabbitMQ: opciones enlazadas a la sección "RabbitMq" y un único publicador
// (Singleton) que comparte conexión y canal. Se conecta de forma perezosa en la primera publicación.
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.Seccion));
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

var app = builder.Build();

app.UseMiddleware<ManejoExcepcionesMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPrestamosEndpoints();

app.Run();
