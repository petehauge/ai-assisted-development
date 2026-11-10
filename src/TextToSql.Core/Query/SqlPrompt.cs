namespace TextToSql.Core.Query;

/// <summary>The prompt sent to the model. It contains only the instructions, the schema text and the question: never row data, connection strings or secrets.</summary>
public static class SqlPrompt
{
    public const string Instructions = """
        You translate questions about a Microsoft Azure SQL database into a single T-SQL statement.

        Rules:
        - Use only the tables and columns listed in the schema below, and qualify every table with its schema (for example dbo.Customers).
        - Write T-SQL for Azure SQL: use TOP instead of LIMIT, and square brackets when an identifier needs quoting.
        - Give result columns clear names with AS when they are computed (for example COUNT(*) AS CustomerCount).
        - If the question asks to add, change or delete data or tables, write the statement that does it.
        - Reply with the SQL statement only: no explanation and no markdown code fences.

        Schema (one table per line; columns are NOT NULL unless marked NULL; PK = primary key; FK->table.column = foreign key):
        """;

    public static string Build(string schemaText, string question) =>
        $"{Instructions}\n{schemaText.TrimEnd()}\n\nQuestion: {question.Trim()}";
}
