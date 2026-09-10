using Microsoft.EntityFrameworkCore;

namespace LibrosAPI.Models;

public class LibrosDbContext(DbContextOptions<LibrosDbContext> options) : DbContext(options)
{
    public DbSet<Libro> Libros => Set<Libro>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Libro>().HasData(
            new Libro { Id = 1, Titulo = "Cien años de soledad", Autor = "Gabriel García Márquez", AnioPublicacion = 1967 },
            new Libro { Id = 2, Titulo = "Don Quijote de la Mancha", Autor = "Miguel de Cervantes", AnioPublicacion = 1605 },
            new Libro { Id = 3, Titulo = "El amor en los tiempos del cólera", Autor = "Gabriel García Márquez", AnioPublicacion = 1985 });
    }
}
