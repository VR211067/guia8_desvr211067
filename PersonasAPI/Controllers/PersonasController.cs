using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonasAPI.Models;

namespace PersonasAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonasController(PersonasDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Persona>>> GetPersonas() =>
        await context.Personas.AsNoTracking().OrderBy(p => p.Id).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Persona>> GetPersona(int id)
    {
        var persona = await context.Personas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return persona is null ? NotFound() : persona;
    }

    [HttpPost]
    public async Task<ActionResult<Persona>> PostPersona(Persona persona)
    {
        if (!Validar(persona)) return BadRequest(new ValidationProblemDetails(ModelState));
        if (persona.Id != 0) return BadRequest("El Id es generado por la base de datos.");
        context.Personas.Add(persona);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPersona), new { id = persona.Id }, persona);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutPersona(int id, Persona persona)
    {
        if (id != persona.Id) return BadRequest("El Id de la ruta y del cuerpo deben coincidir.");
        if (!Validar(persona)) return BadRequest(new ValidationProblemDetails(ModelState));
        var actual = await context.Personas.FindAsync(id);
        if (actual is null) return NotFound();
        context.Entry(actual).CurrentValues.SetValues(persona);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePersona(int id)
    {
        var persona = await context.Personas.FindAsync(id);
        if (persona is null) return NotFound();
        context.Personas.Remove(persona);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private bool Validar(Persona persona)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(persona, new ValidationContext(persona), errores, validateAllProperties: true);
        foreach (var error in errores)
            foreach (var campo in error.MemberNames)
                ModelState.AddModelError(campo, error.ErrorMessage ?? "Valor inválido.");
        return ModelState.IsValid;
    }
}
