using Catalogo.Api.Endpoints;
using Catalogo.Api.Middleware;
using Catalogo.Application.IRepository;
using Catalogo.Application.Services;
using Catalogo.Infrastructure.Repository;

// ---------- Composition root ----------
var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Los errores de lectura del body (JSON inválido, tipos incorrectos) se lanzan como excepción
// también fuera de Development, para que los traduzca el middleware.
builder.Services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = true);

// Infrastructure: Singleton para conservar los datos en memoria entre peticiones.
builder.Services.AddSingleton<IRepositorioLibros, LibroRepositoryEnMemoria>();
// Application: un servicio por petición.
builder.Services.AddScoped<ServicioLibros>();

var app = builder.Build();

app.UseMiddleware<ManejoExcepcionesMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapLibrosEndpoints();

app.Run();
