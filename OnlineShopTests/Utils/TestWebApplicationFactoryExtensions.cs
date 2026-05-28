using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using online_shop_IT.Data;

namespace OnlineShopTests.Utils;

public static class TestWebApplicationFactoryExtensions
{
    public static WebApplicationFactory<TProgram> WithTestDatabase<TProgram>(
        this WebApplicationFactory<TProgram> factory) where TProgram : class
    {
        // Use a unique temp SQLite file per factory instance so both environments
        // use the same provider — avoids the "two providers registered" EF Core error
        var testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");

        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(ApplicationDbContext) ||
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                        d.ServiceType == typeof(DbContextOptions))
                    .ToList();
                foreach (var d in toRemove)
                    services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite($"Data Source={testDbPath}"));
            });
        });
    }
}
