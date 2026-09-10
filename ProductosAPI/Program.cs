using Microsoft.EntityFrameworkCore;
using ProductosAPI.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();
builder.Services.AddDbContext<ProductosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection")
    ?? throw new InvalidOperationException("Falta RedisConnection.");
builder.Services.AddStackExchangeRedisOutputCache(options => options.Configuration = redisConnection);
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddOutputCache();
builder.Services.AddControllers();

var app = builder.Build();
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProductosDbContext>();
    await db.Database.MigrateAsync();
    
    Console.WriteLine("Base de datos migrada y siembra completada.");
    return;
}
app.UseAuthorization();
app.UseOutputCache();
app.MapControllers();
app.Run();
