using Microsoft.EntityFrameworkCore;
using PersonasAPI.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<PersonasDbContext>(options =>
    options.UseInMemoryDatabase(builder.Configuration["Database:Name"] ?? "PersonasInMemoryDb"));
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();

public partial class Program { }
