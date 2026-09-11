using Microsoft.EntityFrameworkCore;

namespace PersonasAPI.Models;

public class PersonasDbContext(DbContextOptions<PersonasDbContext> options) : DbContext(options)
{
    public DbSet<Persona> Personas => Set<Persona>();
}
