using Microsoft.AspNetCore.Mvc;
using PersonasAPI.Controllers;
using PersonasAPI.Models;

namespace PersonasAPI.Tests;

public class PersonasControllerTest
{
    [Fact]
    public async Task PostPersona_CreaPersona_ConCamposOpcionalesNulos()
    {
        using var context = Setup.GetDatabaseContext();
        var controller = new PersonasController(context);
        var result = await controller.PostPersona(Setup.PersonaValida());
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var persona = Assert.IsType<Persona>(created.Value);
        Assert.Equal(nameof(controller.GetPersona), created.ActionName);
        Assert.Equal(persona.Id, created.RouteValues!["id"]);
        Assert.Null(persona.SegundoNombre);
        Assert.Null(persona.SegundoApellido);
        Assert.Single(context.Personas);
    }

    [Theory]
    [InlineData("PrimerNombre", null)]
    [InlineData("PrimerNombre", "")]
    [InlineData("PrimerNombre", "   ")]
    [InlineData("PrimerApellido", null)]
    [InlineData("PrimerApellido", "")]
    [InlineData("PrimerApellido", "   ")]
    public async Task PostPersona_RechazaCamposRequeridosVacios(string campo, string? valor)
    {
        using var context = Setup.GetDatabaseContext();
        var controller = new PersonasController(context);
        var persona = Setup.PersonaValida();
        typeof(Persona).GetProperty(campo)!.SetValue(persona, valor);
        var result = await controller.PostPersona(persona);
        Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>(result.Result).StatusCode);
        Assert.True(controller.ModelState.ContainsKey(campo));
        Assert.Empty(context.Personas);
    }

    [Theory]
    [InlineData("PrimerNombre", 100, true)]
    [InlineData("PrimerNombre", 101, false)]
    [InlineData("SegundoNombre", 100, true)]
    [InlineData("SegundoNombre", 101, false)]
    [InlineData("PrimerApellido", 100, true)]
    [InlineData("PrimerApellido", 101, false)]
    [InlineData("SegundoApellido", 100, true)]
    [InlineData("SegundoApellido", 101, false)]
    public async Task PostPersona_ValidaLimiteDeCaracteres(string campo, int longitud, bool valido)
    {
        using var context = Setup.GetDatabaseContext();
        var controller = new PersonasController(context);
        var persona = Setup.PersonaValida();
        typeof(Persona).GetProperty(campo)!.SetValue(persona, new string('A', longitud));
        var result = await controller.PostPersona(persona);
        if (valido) Assert.IsType<CreatedAtActionResult>(result.Result);
        else
        {
            Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>(result.Result).StatusCode);
            Assert.Empty(context.Personas);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("012345678")]
    [InlineData("1234567-8")]
    [InlineData("01234567-A")]
    [InlineData("012345678-9")]
    [InlineData(" 01234567-8")]
    [InlineData("abcdefgh-1")]
    public async Task PostPersona_RechazaDuiInvalido(string? dui)
    {
        using var context = Setup.GetDatabaseContext();
        var controller = new PersonasController(context);
        var persona = Setup.PersonaValida();
        persona.DUI = dui!;
        var result = await controller.PostPersona(persona);
        Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>(result.Result).StatusCode);
        Assert.True(controller.ModelState.ContainsKey(nameof(Persona.DUI)));
        Assert.Empty(context.Personas);
    }

    [Fact]
    public async Task PostPersona_RechazaFechaNula()
    {
        using var context = Setup.GetDatabaseContext();
        var controller = new PersonasController(context);
        var persona = Setup.PersonaValida();
        persona.FechaNacimiento = null;
        var result = await controller.PostPersona(persona);
        Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>(result.Result).StatusCode);
        Assert.Empty(context.Personas);
    }

    [Fact]
    public async Task GetPersona_RetornaPersona_CuandoExiste()
    {
        using var context = Setup.GetDatabaseContext();
        var persona = Setup.PersonaValida();
        context.Personas.Add(persona);
        await context.SaveChangesAsync();
        var result = await new PersonasController(context).GetPersona(persona.Id);
        Assert.Equal(persona.DUI, result.Value!.DUI);
    }

    [Fact]
    public async Task GetPersona_Retorna404_CuandoNoExiste()
    {
        using var context = Setup.GetDatabaseContext();
        Assert.IsType<NotFoundResult>((await new PersonasController(context).GetPersona(999)).Result);
    }

    [Fact]
    public async Task GetPersonas_RetornaRegistros()
    {
        using var context = Setup.GetDatabaseContext();
        context.Personas.Add(Setup.PersonaValida());
        await context.SaveChangesAsync();
        Assert.Single((await new PersonasController(context).GetPersonas()).Value!);
    }

    [Fact]
    public async Task PutPersona_ActualizaDatos()
    {
        using var context = Setup.GetDatabaseContext();
        var original = Setup.PersonaValida();
        context.Personas.Add(original);
        await context.SaveChangesAsync();
        var cambio = Setup.PersonaValida();
        cambio.Id = original.Id;
        cambio.PrimerNombre = "María";
        Assert.IsType<NoContentResult>(await new PersonasController(context).PutPersona(original.Id, cambio));
        Assert.Equal("María", (await context.Personas.FindAsync(original.Id))!.PrimerNombre);
    }

    [Fact]
    public async Task PutPersona_RechazaDatosInvalidos_SinModificarRegistro()
    {
        using var context = Setup.GetDatabaseContext();
        var original = Setup.PersonaValida();
        context.Personas.Add(original);
        await context.SaveChangesAsync();
        var cambio = Setup.PersonaValida();
        cambio.Id = original.Id;
        cambio.PrimerNombre = "";
        var result = await new PersonasController(context).PutPersona(original.Id, cambio);
        Assert.Equal(400, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
        Assert.Equal("Ana", (await context.Personas.FindAsync(original.Id))!.PrimerNombre);
    }

    [Fact]
    public async Task PutPersona_RechazaIdDiferente()
    {
        using var context = Setup.GetDatabaseContext();
        Assert.IsType<BadRequestObjectResult>(await new PersonasController(context).PutPersona(99, Setup.PersonaValida()));
    }

    [Fact]
    public async Task PutPersona_Retorna404_CuandoNoExiste()
    {
        using var context = Setup.GetDatabaseContext();
        var persona = Setup.PersonaValida();
        persona.Id = 99;
        Assert.IsType<NotFoundResult>(await new PersonasController(context).PutPersona(99, persona));
    }

    [Fact]
    public async Task DeletePersona_EliminaRegistro()
    {
        using var context = Setup.GetDatabaseContext();
        var persona = Setup.PersonaValida();
        context.Personas.Add(persona);
        await context.SaveChangesAsync();
        Assert.IsType<NoContentResult>(await new PersonasController(context).DeletePersona(persona.Id));
        Assert.Empty(context.Personas);
    }

    [Fact]
    public async Task DeletePersona_Retorna404_CuandoNoExiste()
    {
        using var context = Setup.GetDatabaseContext();
        Assert.IsType<NotFoundResult>(await new PersonasController(context).DeletePersona(99));
    }
}
