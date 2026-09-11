using System.Text.Json;
using LibrosAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StackExchange.Redis;

namespace LibrosAPI.Tests;

public class LibrosControllerTest
{
    private static Libro NuevoLibro() => new() { Titulo = "Libro de prueba", Autor = "Autor de prueba", AnioPublicacion = 2026 };

    [Fact]
    public async Task PostLibro_AgregarLibro_CuandoLibroEsValido()
    {
        using var context = Setup.GetDatabaseContext();
        var controller = Setup.GetController(context, out var cache);
        var result = await controller.PostLibro(NuevoLibro());
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var libro = Assert.IsType<Libro>(created.Value);
        Assert.Equal("Libro de prueba", libro.Titulo);
        Assert.Equal(nameof(controller.GetLibro), created.ActionName);
        Assert.Equal(libro.Id, created.RouteValues!["id"]);
        Assert.True(await context.Libros.AnyAsync(x => x.Id == libro.Id));
        cache.Verify(x => x.KeyDeleteAsync("libros_list", CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task GetLibro_RetornaLibro_CuandoIdEsValido()
    {
        using var context = Setup.GetDatabaseContext();
        var libro = NuevoLibro();
        context.Libros.Add(libro);
        await context.SaveChangesAsync();
        var controller = Setup.GetController(context, out var cache);
        var result = await controller.GetLibro(libro.Id);
        Assert.Equal(libro.Titulo, Assert.IsType<Libro>(result.Value).Titulo);
        Assert.Equal("MISS", controller.Response.Headers["X-Cache"].ToString());
        var write = Assert.Single(cache.Invocations, x => x.Method.Name == "StringSetAsync");
        Assert.Equal($"libro_{libro.Id}", write.Arguments[0].ToString());
        Assert.Equal(TimeSpan.FromMinutes(10), write.Arguments[2]);
    }

    [Fact]
    public async Task GetLibro_RetornaNotFound_CuandoIdNoExiste()
    {
        using var context = Setup.GetDatabaseContext();
        var controller = Setup.GetController(context, out _);
        Assert.IsType<NotFoundResult>((await controller.GetLibro(999)).Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostLibro_NoAgregarLibro_CuandoNoTieneTitulo(string? titulo)
    {
        using var context = Setup.GetDatabaseContext();
        var controller = Setup.GetController(context, out var cache);
        var libro = NuevoLibro();
        libro.Titulo = titulo!;
        var result = await controller.PostLibro(libro);
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(context.Libros);
        cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PostLibro_IncrementaConteo_CuandoSeAgregaNuevoLibro()
    {
        using var context = Setup.GetDatabaseContext();
        var controller = Setup.GetController(context, out _);
        await controller.PostLibro(NuevoLibro());
        await controller.PostLibro(NuevoLibro());
        Assert.Equal(2, await context.Libros.CountAsync());
    }

    [Fact]
    public async Task GetLibro_RetornaCache_SinConsultarBaseDeDatos()
    {
        using var context = Setup.GetDatabaseContext();
        var controller = Setup.GetController(context, out var cache);
        var libro = NuevoLibro();
        libro.Id = 42;
        cache.Setup(x => x.StringGetAsync("libro_42", CommandFlags.None))
            .ReturnsAsync(JsonSerializer.Serialize(libro));
        context.Dispose();
        var result = await controller.GetLibro(42);
        Assert.Equal(42, result.Value!.Id);
        Assert.Equal("HIT", controller.Response.Headers["X-Cache"].ToString());
    }

    [Fact]
    public async Task GetLibros_RetornaListaDesdeCache()
    {
        using var context = Setup.GetDatabaseContext();
        var controller = Setup.GetController(context, out var cache);
        cache.Setup(x => x.StringGetAsync("libros_list", CommandFlags.None))
            .ReturnsAsync(JsonSerializer.Serialize(new[] { NuevoLibro() }));
        context.Dispose();
        Assert.Single((await controller.GetLibros()).Value!);
        Assert.Equal("HIT", controller.Response.Headers["X-Cache"].ToString());
    }

    [Fact]
    public async Task PutLibro_ActualizaEInvalidaListaYDetalle()
    {
        using var context = Setup.GetDatabaseContext();
        var libro = NuevoLibro();
        context.Libros.Add(libro);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var controller = Setup.GetController(context, out var cache);
        var cambio = NuevoLibro();
        cambio.Id = libro.Id;
        cambio.Titulo = "Actualizado";
        Assert.IsType<NoContentResult>(await controller.PutLibro(libro.Id, cambio));
        Assert.Equal("Actualizado", (await context.Libros.FindAsync(libro.Id))!.Titulo);
        cache.Verify(x => x.KeyDeleteAsync(It.Is<RedisKey[]>(keys =>
            keys.Contains((RedisKey)"libros_list") && keys.Contains((RedisKey)$"libro_{libro.Id}")), CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task DeleteLibro_EliminaEInvalidaListaYDetalle()
    {
        using var context = Setup.GetDatabaseContext();
        var libro = NuevoLibro();
        context.Libros.Add(libro);
        await context.SaveChangesAsync();
        var controller = Setup.GetController(context, out var cache);
        Assert.IsType<NoContentResult>(await controller.DeleteLibro(libro.Id));
        Assert.Null(await context.Libros.FindAsync(libro.Id));
        cache.Verify(x => x.KeyDeleteAsync(It.Is<RedisKey[]>(keys =>
            keys.Contains((RedisKey)"libros_list") && keys.Contains((RedisKey)$"libro_{libro.Id}")), CommandFlags.None), Times.Once);
    }
}
