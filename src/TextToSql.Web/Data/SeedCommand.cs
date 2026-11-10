using Microsoft.Data.SqlClient;

namespace TextToSql.Web.Data;

/// <summary>The <c>seed</c> command-line verb: recreates the sample tables, prints their row counts, and returns a process exit code.</summary>
public static class SeedCommand
{
    public static async Task<int> RunAsync(IServiceProvider services, ILogger logger)
    {
        try
        {
            var counts = await services.GetRequiredService<DatabaseSeeder>().SeedAsync(CancellationToken.None);
            Console.WriteLine("Seeded sample database:");
            foreach (var count in counts)
            {
                Console.WriteLine($"  {count.Table,-12} {count.Rows,5} rows");
            }

            return 0;
        }
        catch (Exception ex) when (ex is SqlException or MissingConfigurationException)
        {
            logger.LogError(ex, "Seeding failed");
            Console.Error.WriteLine($"Seeding failed: {ex.Message}");
            return 1;
        }
    }
}
