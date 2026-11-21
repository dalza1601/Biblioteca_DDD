using System.Text;
using System.Text.Json;
using Prestamos.Api.Integrations.Mensajeria.Events;

namespace Biblioteca.Tests.Mensajeria;

public class PrestamoRegistradoTests
{
    [Fact]
    public void El_contrato_se_serializa_con_cinco_campos_camelCase_fecha_UTC_y_sin_datos_del_lector()
    {
        var evento = PrestamoRegistrado.Crear(prestamoId: 12, libroId: 4);

        using var json = JsonDocument.Parse(evento.Serializar());
        var campos = json.RootElement.EnumerateObject().Select(p => p.Name).ToArray();

        Assert.Equal(["eventoId", "prestamoId", "libroId", "fechaRegistroUtc", "version"], campos);
        Assert.All(campos, campo => Assert.True(char.IsLower(campo[0]), $"'{campo}' no está en camelCase"));

        var fecha = json.RootElement.GetProperty("fechaRegistroUtc").GetString()!;
        Assert.EndsWith("Z", fecha); // UTC explícito
        Assert.Equal(DateTimeKind.Utc, json.RootElement.GetProperty("fechaRegistroUtc").GetDateTime().ToUniversalTime().Kind);

        Assert.DoesNotContain("lector", json.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EST-", json.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Crear_genera_un_EventoId_nuevo_hora_UTC_actual_y_version_1()
    {
        var antes = DateTime.UtcNow;

        var evento = PrestamoRegistrado.Crear(prestamoId: 10, libroId: 3);
        var otro = PrestamoRegistrado.Crear(prestamoId: 10, libroId: 3);

        Assert.NotEqual(Guid.Empty, evento.EventoId);
        Assert.NotEqual(evento.EventoId, otro.EventoId);
        Assert.Equal(10, evento.PrestamoId);
        Assert.Equal(3, evento.LibroId);
        Assert.Equal(DateTimeKind.Utc, evento.FechaRegistroUtc.Kind);
        Assert.InRange(evento.FechaRegistroUtc, antes, DateTime.UtcNow);
        Assert.Equal(1, evento.Version);
        Assert.Equal("prestamo.registrado", PrestamoRegistrado.RoutingKey);
    }

    [Fact]
    public void Serializar_produce_JSON_en_camelCase_sin_el_lector_ni_la_routing_key()
    {
        var evento = new PrestamoRegistrado(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), 10, 3, new DateTime(2026, 11, 21, 15, 30, 0, DateTimeKind.Utc));

        var json = Encoding.UTF8.GetString(evento.Serializar());

        Assert.Equal(
            """{"eventoId":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","prestamoId":10,"libroId":3,"fechaRegistroUtc":"2026-11-21T15:30:00Z","version":1}""",
            json);
        Assert.DoesNotContain("lector", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void El_JSON_se_puede_leer_de_vuelta_con_las_mismas_opciones()
    {
        var evento = PrestamoRegistrado.Crear(prestamoId: 7, libroId: 2);

        var leido = JsonSerializer.Deserialize<PrestamoRegistrado>(evento.Serializar(), PrestamoRegistrado.OpcionesJson);

        Assert.Equal(evento, leido);
    }
}
