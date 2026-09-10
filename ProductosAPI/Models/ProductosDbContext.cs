using Microsoft.EntityFrameworkCore;
namespace ProductosAPI.Models;

public class ProductosDbContext(DbContextOptions<ProductosDbContext> options) : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Producto>().HasData(
 new Producto
 {
     Id = 1,
     Nombre = "Laptop",
     Categoria = "Electrónica",
     Descripcion = "Laptop de alto rendimiento"
 },
 new Producto
 {
     Id = 2,
     Nombre = "Smartphone",
     Categoria = "Electrónica",
     Descripcion = "Smartphone de última generación"
 },
 new Producto
 {
     Id = 3,
     Nombre = "Silla de escritorio",
     Categoria = "Muebles",
     Descripcion = "Silla de escritorio ejecutivo"
 }
 );
    }
}
