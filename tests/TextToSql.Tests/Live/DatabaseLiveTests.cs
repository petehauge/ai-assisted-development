using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using TextToSql.Core.Schema;
using TextToSql.Web.Data;

namespace TextToSql.Tests.Live;

/// <summary>Runs against the real Azure SQL database (Entra ID via the developer's az login). Seeding resets the sample tables.</summary>
[Trait("Category", "Live")]
[Collection(LiveDatabaseCollection.Name)]
public sealed class DatabaseLiveTests
{
    private readonly SqlConnectionFactory _connections = new(LiveConfiguration.Current);

    [LiveFact(SqlConnectionFactory.ConnectionStringKey)]
    public async Task Seeding_twice_recreates_the_same_data() // AC1
    {
        var seeder = new DatabaseSeeder(_connections, NullLogger<DatabaseSeeder>.Instance);

        var first = await seeder.SeedAsync(CancellationToken.None);
        var firstChecksum = await DataChecksumAsync();
        var second = await seeder.SeedAsync(CancellationToken.None);
        var secondChecksum = await DataChecksumAsync();

        Assert.Equal(first, second);
        Assert.Equal(firstChecksum, secondChecksum);
        Assert.Contains(new TableRowCount("Customers", 100), second);
    }

    [LiveFact(SqlConnectionFactory.ConnectionStringKey)]
    public async Task Schema_contains_all_sample_tables_with_keys() // AC2
    {
        await new DatabaseSeeder(_connections, NullLogger<DatabaseSeeder>.Instance).SeedAsync(CancellationToken.None);
        var provider = new SqlSchemaProvider(_connections, NullLogger<SqlSchemaProvider>.Instance);

        var schema = await provider.GetSchemaAsync(CancellationToken.None);
        var text = SchemaFormatter.Format(schema);

        foreach (var table in new[] { "Categories", "Products", "Customers", "Orders", "OrderItems" })
        {
            Assert.Contains(schema.Tables, t => t.QualifiedName == $"dbo.{table}");
        }

        Assert.Contains("dbo.Products(ProductId int PK, Name nvarchar(100), CategoryId int FK->dbo.Categories.CategoryId, UnitPrice decimal(10,2), Discontinued bit)", text);
        Assert.Contains("OrderId int FK->dbo.Orders.OrderId", text);
        Assert.Contains("ProductId int FK->dbo.Products.ProductId", text);
    }

    // A fingerprint of every seeded row, so "same data" means more than "same counts".
    private async Task<string> DataChecksumAsync()
    {
        await using var connection = _connections.Create();
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT CONCAT(
                (SELECT CHECKSUM_AGG(CHECKSUM(*)) FROM dbo.Categories), '|',
                (SELECT CHECKSUM_AGG(CHECKSUM(*)) FROM dbo.Products), '|',
                (SELECT CHECKSUM_AGG(CHECKSUM(*)) FROM dbo.Customers), '|',
                (SELECT CHECKSUM_AGG(CHECKSUM(*)) FROM dbo.Orders), '|',
                (SELECT CHECKSUM_AGG(CHECKSUM(*)) FROM dbo.OrderItems))
            """, connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }
}

/// <summary>Live database tests reseed shared tables, so they must not run in parallel with each other.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LiveDatabaseCollection
{
    public const string Name = "Live database";
}
