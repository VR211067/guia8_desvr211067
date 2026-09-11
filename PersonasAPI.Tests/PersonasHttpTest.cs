using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;

namespace PersonasAPI.Tests;

public class PersonasHttpTest
{
    private static WebApplicationFactory<Program> CreateApplication() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Database:Name", Guid.NewGuid().ToString()));
    [Theory]
    [InlineData("2025-02-29")]
    [InlineData("2000-13-01")]
    [InlineData("2000-04-31")]
    [InlineData("no-es-fecha")]
    public async Task PostPersona_RechazaFechaImposibleEnJson(string fecha)
    {
        using var app = CreateApplication();
        using var client = app.CreateClient();
        var json = $$"""{"primerNombre":"Ana","primerApellido":"Pérez","dui":"01234567-8","fechaNacimiento":"{{fecha}}"}""";
        var response = await client.PostAsync("/api/Personas", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("[]", await client.GetStringAsync("/api/Personas"));
    }

    [Fact]
    public async Task PostPersona_AceptaFechaBisiestaValida_YPermiteConsultarLocation()
    {
        using var app = CreateApplication();
        using var client = app.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Personas", Setup.PersonaValida());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var detail = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }
}
