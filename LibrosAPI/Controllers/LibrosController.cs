using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibrosAPI.Models;
using StackExchange.Redis;

namespace LibrosAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LibrosController(LibrosDbContext context, IConnectionMultiplexer redis) : ControllerBase
{
    private const string ListKey = "libros_list";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static string ItemKey(int id) => $"libro_{id}";

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Libro>>> GetLibros()
    {
        var cache = redis.GetDatabase();
        var value = await cache.StringGetAsync(ListKey);
        if (!value.IsNullOrEmpty)
        {
            var cached = JsonSerializer.Deserialize<List<Libro>>(value.ToString());
            if (cached is not null)
            {
                Response.Headers["X-Cache"] = "HIT";
                return cached;
            }
        }
        var items = await context.Libros.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        await cache.StringSetAsync(ListKey, JsonSerializer.Serialize(items), CacheDuration);
        Response.Headers["X-Cache"] = "MISS";
        return items;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Libro>> GetLibro(int id)
    {
        var cache = redis.GetDatabase();
        var value = await cache.StringGetAsync(ItemKey(id));
        if (!value.IsNullOrEmpty)
        {
            var cached = JsonSerializer.Deserialize<Libro>(value.ToString());
            if (cached is not null)
            {
                Response.Headers["X-Cache"] = "HIT";
                return cached;
            }
        }
        var item = await context.Libros.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        await cache.StringSetAsync(ItemKey(id), JsonSerializer.Serialize(item), CacheDuration);
        Response.Headers["X-Cache"] = "MISS";
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Libro>> PostLibro(Libro item)
    {
        if (item.Id != 0) return BadRequest("El Id es generado por la base de datos.");
        context.Libros.Add(item);
        await context.SaveChangesAsync();
        await redis.GetDatabase().KeyDeleteAsync(ListKey);
        return CreatedAtAction(nameof(GetLibro), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutLibro(int id, Libro item)
    {
        if (id != item.Id) return BadRequest("El Id de la ruta y del cuerpo deben coincidir.");
        context.Entry(item).State = EntityState.Modified;
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.Libros.AnyAsync(x => x.Id == id)) return NotFound();
            throw;
        }
        await InvalidateAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLibro(int id)
    {
        var item = await context.Libros.FindAsync(id);
        if (item is null) return NotFound();
        context.Libros.Remove(item);
        await context.SaveChangesAsync();
        await InvalidateAsync(id);
        return NoContent();
    }

    private async Task InvalidateAsync(int id) =>
        await redis.GetDatabase().KeyDeleteAsync(new RedisKey[] { ListKey, ItemKey(id) });
}
