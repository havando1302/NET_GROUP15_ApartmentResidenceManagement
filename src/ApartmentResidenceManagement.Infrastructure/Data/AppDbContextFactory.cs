using System;
using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ApartmentResidenceManagement.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DotNetEnv.Env.NoClobber().TraversePath().Load();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ApartmentResidenceManagement.Wpf", "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var csElement) &&
                    csElement.TryGetProperty("DefaultConnection", out var defConn))
                {
                    connectionString = defConn.GetString();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=mysql-24ea5697-hado5201314-2be8.b.aivencloud.com;Port=28269;Database=defaultdb;User Id=avnadmin;Password=NetGroup15_2026!;SslMode=Required";
        }

        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));
        optionsBuilder.UseMySql(connectionString, serverVersion);

        return new AppDbContext(optionsBuilder.Options);
    }
}
