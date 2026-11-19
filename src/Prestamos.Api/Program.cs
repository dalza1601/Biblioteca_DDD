using Prestamos.Api;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddManejoErrores();
builder.Services.AddSingleton<RepositorioPrestamos>();

var app = builder.Build();

app.UseManejoErrores();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPrestamosEndpoints();

app.Run();
