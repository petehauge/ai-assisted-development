using TextToSql.Core.Query;

namespace TextToSql.Tests.Core;

public sealed class SqlExtractorTests // AC4
{
    [Fact]
    public void Plain_sql_is_returned_trimmed()
    {
        Assert.Equal("SELECT COUNT(*) FROM dbo.Customers;", SqlExtractor.Extract("  SELECT COUNT(*) FROM dbo.Customers;\n"));
    }

    [Theory]
    [InlineData("```sql\nSELECT 1;\n```")]
    [InlineData("```tsql\r\nSELECT 1;\r\n```")]
    [InlineData("```\nSELECT 1;\n```")]
    [InlineData("Here is the query:\n\n```sql\nSELECT 1;\n```\nThis counts rows.")]
    public void Fenced_sql_is_returned_without_fences_or_commentary(string reply)
    {
        Assert.Equal("SELECT 1;", SqlExtractor.Extract(reply));
    }

    [Fact]
    public void Commentary_before_unfenced_sql_is_removed()
    {
        var reply = "Sure, this query answers the question:\nWITH t AS (SELECT 1 AS x)\nSELECT x FROM t;";

        Assert.Equal("WITH t AS (SELECT 1 AS x)\nSELECT x FROM t;", SqlExtractor.Extract(reply));
    }

    [Fact]
    public void Multi_line_fenced_statement_is_kept_intact()
    {
        var reply = "```sql\nSELECT TOP (5) p.Name\nFROM dbo.Products AS p\nORDER BY p.UnitPrice DESC;\n```";

        Assert.Equal("SELECT TOP (5) p.Name\nFROM dbo.Products AS p\nORDER BY p.UnitPrice DESC;", SqlExtractor.Extract(reply));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_reply_gives_empty_sql(string? reply)
    {
        Assert.Equal(string.Empty, SqlExtractor.Extract(reply));
    }
}
