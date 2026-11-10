namespace TextToSql.Web.Configuration;

/// <summary>Result of looking for the local secrets file. <see cref="Path"/> is null when no file was found; <see cref="Description"/> explains what happened.</summary>
public sealed record SecretsLookup(string? Path, string Description);

/// <summary>
/// Finds <c>secrets.json</c> for local development, following the rule in CLAUDE.md:
/// use the <c>SECRETS_PATH</c> environment variable if it is set; otherwise walk up from the start directory to the repo root (the first folder containing a <c>.git</c> folder or file).
/// If the repo root is a git worktree (<c>.git</c> is a file) and has no secrets file, look in the main checkout that the worktree's <c>gitdir:</c> line points to.
/// </summary>
public static class SecretsFileLocator
{
    public const string FileName = "secrets.json";
    public const string EnvironmentVariable = "SECRETS_PATH";

    public static SecretsLookup Locate(string? secretsPathVariable, string startDirectory)
    {
        if (!string.IsNullOrWhiteSpace(secretsPathVariable))
        {
            var full = System.IO.Path.GetFullPath(secretsPathVariable);
            return File.Exists(full)
                ? new SecretsLookup(full, $"Using {EnvironmentVariable}: {full}")
                : new SecretsLookup(null, $"{EnvironmentVariable} is set to '{full}', but that file does not exist.");
        }

        var repoRoot = FindRepoRoot(startDirectory);
        if (repoRoot is null)
        {
            return new SecretsLookup(null, $"No repository root (.git) found above '{startDirectory}', and {EnvironmentVariable} is not set.");
        }

        var candidate = System.IO.Path.Combine(repoRoot, FileName);
        if (File.Exists(candidate))
        {
            return new SecretsLookup(candidate, $"Found {FileName} at the repo root: {candidate}");
        }

        var gitFile = System.IO.Path.Combine(repoRoot, ".git");
        if (File.Exists(gitFile))
        {
            var mainCheckout = ResolveMainCheckout(repoRoot, gitFile);
            if (mainCheckout is not null)
            {
                var mainCandidate = System.IO.Path.Combine(mainCheckout, FileName);
                if (File.Exists(mainCandidate))
                {
                    return new SecretsLookup(mainCandidate, $"Found {FileName} in the main checkout of this worktree: {mainCandidate}");
                }
            }
        }

        return new SecretsLookup(null, $"No {FileName} found at the repo root '{repoRoot}' (or its main checkout).");
    }

    private static string? FindRepoRoot(string startDirectory)
    {
        for (var dir = new DirectoryInfo(System.IO.Path.GetFullPath(startDirectory)); dir is not null; dir = dir.Parent)
        {
            var git = System.IO.Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
            {
                return dir.FullName;
            }
        }

        return null;
    }

    // A worktree's .git file contains "gitdir: <main checkout>/.git/worktrees/<name>" (absolute, or relative to the worktree root).
    private static string? ResolveMainCheckout(string worktreeRoot, string gitFile)
    {
        var line = File.ReadLines(gitFile).FirstOrDefault(l => l.StartsWith("gitdir:", StringComparison.Ordinal));
        if (line is null)
        {
            return null;
        }

        var gitDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(worktreeRoot, line["gitdir:".Length..].Trim()));
        // <main>/.git/worktrees/<name> -> <main>
        return new DirectoryInfo(gitDir).Parent?.Parent?.Parent?.FullName;
    }
}
