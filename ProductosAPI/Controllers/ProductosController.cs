using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductosAPI.Models;
using StackExchange.Redis;

namespace ProductosAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController(ProductosDbContext context, IConnectionMultiplexer redis) : ControllerBase
{
    private const string ListKey = "productos_list";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static string ItemKey(int id) => $"producto_{id}";

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
    {
        var cache = redis.GetDatabase();
        var value = await cache.StringGetAsync(ListKey);
        if (!value.IsNullOrEmpty)
        {
            var cached = JsonSerializer.Deserialize<List<Producto>>(value.ToString());
            if (cached is not null)
            {
                Response.Headers["X-Cache"] = "HIT";
                return cached;
            }
        }
        var items = await context.Productos.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        await cache.StringSetAsync(ListKey, JsonSerializer.Serialize(items), CacheDuration);
        Response.Headers["X-Cache"] = "MISS";
        return items;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Producto>> GetProducto(int id)
    {
        var cache = redis.GetDatabase();
        var value = await cache.StringGetAsync(ItemKey(id));
        if (!value.IsNullOrEmpty)
        {
            var cached = JsonSerializer.Deserialize<Producto>(value.ToString());
            if (cached is not null)
            {
                Response.Headers["X-Cache"] = "HIT";
                return cached;
            }
        }
        var item = await context.Productos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        await cache.StringSetAsync(ItemKey(id), JsonSerializer.Serialize(item), CacheDuration);
        Response.Headers["X-Cache"] = "MISS";
        return item;
    }

    [HttpPost]
    public async Task<ActionResult<Producto>> PostProducto(Producto item)
    {
        if (item.Id != 0) return BadRequest("El Id es generado por la base de datos.");
        context.Productos.Add(item);
        await context.SaveChangesAsync();
        await redis.GetDatabase().KeyDeleteAsync(ListKey);
        return CreatedAtAction(nameof(GetProducto), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutProducto(int id, Producto item)
    {
        if (id != item.Id) return BadRequest("El Id de la ruta y del cuerpo deben coincidir.");
        context.Entry(item).State = EntityState.Modified;
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await context.Productos.AnyAsync(x => x.Id == id)) return NotFound();
            throw;
        }
        await InvalidateAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProducto(int id)
    {
        var item = await context.Productos.FindAsync(id);
        if (item is null) return NotFound();
        context.Productos.Remove(item);
        await context.SaveChangesAsync();
        await InvalidateAsync(id);
        return NoContent();
    }

    private async Task InvalidateAsync(int id) =>
        await redis.GetDatabase().KeyDeleteAsync(new RedisKey[] { ListKey, ItemKey(id) });
}
