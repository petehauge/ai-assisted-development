using TextToSql.Core.Schema;

namespace TextToSql.Tests.Core;

public sealed class SchemaFormatterTests
{
    private static readonly DatabaseSchema Sample = new(
    [
        new TableSchema("dbo", "Orders",
        [
            new ColumnSchema("OrderId", "int", IsNullable: false, IsPrimaryKey: true),
            new ColumnSchema("CustomerId", "int", IsNullable: false, IsPrimaryKey: false, new ForeignKeyReference("dbo.Customers", "CustomerId")),
            new ColumnSchema("Notes", "nvarchar(200)", IsNullable: true, IsPrimaryKey: false),
        ]),
        new TableSchema("dbo", "Customers",
        [
            new ColumnSchema("CustomerId", "int", IsNullable: false, IsPrimaryKey: true),
            new ColumnSchema("Name", "nvarchar(100)", IsNullable: false, IsPrimaryKey: false),
        ]),
    ]);

    [Fact]
    public void Formats_every_table_with_columns_types_and_key_markers()
    {
        var text = SchemaFormatter.Format(Sample);

        var lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(
        [
            "dbo.Customers(CustomerId int PK, Name nvarchar(100))",
            "dbo.Orders(OrderId int PK, CustomerId int FK->dbo.Customers.CustomerId, Notes nvarchar(200) NULL)",
        ], lines);
    }

    [Fact]
    public void Composite_primary_key_marks_each_key_column()
    {
        var schema = new DatabaseSchema(
        [
            new TableSchema("dbo", "OrderItems",
            [
                new ColumnSchema("OrderId", "int", false, true, new ForeignKeyReference("dbo.Orders", "OrderId")),
                new ColumnSchema("LineNumber", "int", false, true),
            ]),
        ]);

        var text = SchemaFormatter.Format(schema).TrimEnd();

        Assert.Equal("dbo.OrderItems(OrderId int PK FK->dbo.Orders.OrderId, LineNumber int PK)", text);
    }
}
