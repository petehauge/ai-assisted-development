using System.Text;

namespace TextToSql.Core.Schema;

/// <summary>
/// Renders a schema as compact prompt text, one table per line:
/// <c>dbo.Orders(OrderId int PK, CustomerId int FK->dbo.Customers.CustomerId, OrderDate date, Notes nvarchar(200) NULL)</c>.
/// Columns are NOT NULL unless marked <c>NULL</c>.
/// </summary>
public static class SchemaFormatter
{
    public static string Format(DatabaseSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var sb = new StringBuilder();
        foreach (var table in schema.Tables.OrderBy(t => t.Schema, StringComparer.OrdinalIgnoreCase).ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.Append(table.QualifiedName).Append('(');
            sb.AppendJoin(", ", table.Columns.Select(FormatColumn));
            sb.Append(')').AppendLine();
        }

        return sb.ToString();
    }

    private static string FormatColumn(ColumnSchema column)
    {
        var sb = new StringBuilder($"{column.Name} {column.DataType}");
        if (column.IsPrimaryKey)
        {
            sb.Append(" PK");
        }

        if (column.ForeignKey is { } fk)
        {
            sb.Append($" FK->{fk.Table}.{fk.Column}");
        }

        if (column.IsNullable)
        {
            sb.Append(" NULL");
        }

        return sb.ToString();
    }
}
