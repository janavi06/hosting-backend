using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

        // Choose provider based on connection string
        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            builder.UseNpgsql(connectionString);
        }
        else
        {
            builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }

        return new ApplicationDbContext(builder.Options);
    }
}
