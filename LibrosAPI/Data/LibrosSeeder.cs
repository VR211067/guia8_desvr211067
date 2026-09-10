using System.Text.Json;
using LibrosAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibrosAPI.Data;

public static class LibrosSeeder
{
    public static async Task SeedAsync(LibrosDbContext db, string contentRoot)
    {
        var json = await File.ReadAllTextAsync(Path.Combine(contentRoot, "Data", "libros.json"));
        var libros = JsonSerializer.Deserialize<List<Libro>>(json)
            ?? throw new InvalidOperationException("No se pudieron leer los libros.");
        if (libros.Count < 100 || libros.Any(x => x.Id != 0 || string.IsNullOrWhiteSpace(x.Titulo)
            || string.IsNullOrWhiteSpace(x.Autor)))
            throw new InvalidOperationException("Se requieren 100 libros válidos sin Id explícito.");
        var existentes = await db.Libros.Select(x => x.Titulo).ToListAsync();
        var titulos = existentes.ToHashSet(StringComparer.Ordinal);
        var nuevos = libros.Where(x => titulos.Add(x.Titulo)).ToList();
        db.Libros.AddRange(nuevos);
        await db.SaveChangesAsync();
        Console.WriteLine($"Libros insertados: {nuevos.Count}. Total: {await db.Libros.CountAsync()}.");
    }
}
