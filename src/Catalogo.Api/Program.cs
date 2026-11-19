using Catalogo.Api;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddManejoErrores();
builder.Services.AddSingleton<RepositorioLibros>();

var app = builder.Build();

app.UseManejoErrores();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapLibrosEndpoints();

app.Run();
