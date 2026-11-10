using TextToSql.Web.Data;

namespace TextToSql.Tests.Web;

public sealed class SqlTypeTextTests
{
    [Theory]
    [InlineData("nvarchar", 100, null, null, "nvarchar(100)")]
    [InlineData("nvarchar", -1, null, null, "nvarchar(max)")]
    [InlineData("varbinary", -1, null, null, "varbinary(max)")]
    [InlineData("decimal", null, (byte)10, 2, "decimal(10,2)")]
    [InlineData("int", null, (byte)10, 0, "int")] // int reports a precision but is written without one
    [InlineData("date", null, null, null, "date")]
    public void Formats_types_as_TSQL(string dataType, int? maxLength, byte? precision, int? scale, string expected)
    {
        Assert.Equal(expected, SqlTypeText.Format(dataType, maxLength, precision, scale));
    }
}
