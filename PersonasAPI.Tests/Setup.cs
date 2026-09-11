using Microsoft.EntityFrameworkCore;
using PersonasAPI.Models;

namespace PersonasAPI.Tests;

public static class Setup
{
    public static PersonasDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<PersonasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new PersonasDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static Persona PersonaValida() => new()
    {
        PrimerNombre = "Ana", PrimerApellido = "Pérez", DUI = "01234567-8",
        FechaNacimiento = new DateOnly(2000, 2, 29)
    };
}
