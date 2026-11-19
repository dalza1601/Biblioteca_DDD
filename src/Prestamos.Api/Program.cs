using Prestamos.Api.Endpoints;
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

var app = builder.Build();

app.UseMiddleware<ManejoExcepcionesMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPrestamosEndpoints();

app.Run();
