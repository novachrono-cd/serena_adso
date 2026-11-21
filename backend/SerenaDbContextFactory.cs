using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WebApplication1;

public sealed class SerenaDbContextFactory : IDesignTimeDbContextFactory<SerenaDbContext>
{
    public SerenaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SERENA_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Set SERENA_CONNECTION_STRING to generate EF migrations.");

        var options = new DbContextOptionsBuilder<SerenaDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new SerenaDbContext(options);
    }
}
