namespace TextToSql.Tests.Configuration;

/// <summary>A unique temporary folder for file-system tests, deleted on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    public string Root { get; } = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "texttosql-tests-" + Guid.NewGuid().ToString("N"))).FullName;

    public string Dir(params string[] parts) => Directory.CreateDirectory(Path.Combine([Root, .. parts])).FullName;

    public string File(string relativePath, string content)
    {
        var path = Path.GetFullPath(Path.Combine(Root, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of a temp folder; a leftover folder must not fail the test run.
        }
    }
}
