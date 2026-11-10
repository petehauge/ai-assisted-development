namespace TextToSql.Core.Schema;

/// <summary>The structure of the target database that the model is allowed to see: tables, columns, types and keys. Never row data.</summary>
public sealed record DatabaseSchema(IReadOnlyList<TableSchema> Tables);

public sealed record TableSchema(string Schema, string Name, IReadOnlyList<ColumnSchema> Columns)
{
    public string QualifiedName => $"{Schema}.{Name}";
}

/// <param name="DataType">The SQL type as written in T-SQL, for example <c>int</c>, <c>nvarchar(100)</c> or <c>decimal(10,2)</c>.</param>
public sealed record ColumnSchema(string Name, string DataType, bool IsNullable, bool IsPrimaryKey, ForeignKeyReference? ForeignKey = null);

/// <param name="Table">The referenced table, qualified with its schema (for example <c>dbo.Customers</c>).</param>
public sealed record ForeignKeyReference(string Table, string Column);

public interface ISchemaProvider
{
    Task<DatabaseSchema> GetSchemaAsync(CancellationToken cancellationToken);
}
