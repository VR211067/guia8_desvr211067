using LibrosAPI.Controllers;
using LibrosAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StackExchange.Redis;

namespace LibrosAPI.Tests;

public static class Setup
{
    public static LibrosDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<LibrosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new LibrosDbContext(options);
        context.Database.EnsureCreated();
        context.Libros.RemoveRange(context.Libros);
        context.SaveChanges();
        return context;
    }

    public static LibrosController GetController(LibrosDbContext context, out Mock<IDatabase> cache)
    {
        cache = new Mock<IDatabase>();
        cache.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(cache.Object);
        return new LibrosController(context, redis.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
