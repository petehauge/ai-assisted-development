using TextToSql.Web.Configuration;

namespace TextToSql.Tests.Configuration;

public sealed class SecretsFileLocatorTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Uses_SECRETS_PATH_when_set_even_if_repo_has_its_own_file()
    {
        _temp.Dir("repo", ".git");
        _temp.File("repo/secrets.json", "{}");
        var explicitPath = _temp.File("elsewhere/my-secrets.json", "{}");

        var result = SecretsFileLocator.Locate(explicitPath, _temp.Dir("repo", "src", "app", "bin"));

        Assert.Equal(explicitPath, result.Path);
    }

    [Fact]
    public void SECRETS_PATH_pointing_to_missing_file_returns_not_found_with_reason()
    {
        var missing = Path.Combine(_temp.Root, "nope.json");

        var result = SecretsFileLocator.Locate(missing, _temp.Root);

        Assert.Null(result.Path);
        Assert.Contains("SECRETS_PATH", result.Description);
    }

    [Fact]
    public void Walks_up_to_folder_containing_git_directory()
    {
        _temp.Dir("repo", ".git");
        var expected = _temp.File("repo/secrets.json", "{}");

        var result = SecretsFileLocator.Locate(null, _temp.Dir("repo", "src", "TextToSql.Web", "bin", "Debug", "net10.0"));

        Assert.Equal(expected, result.Path);
    }

    [Fact]
    public void Stops_at_repo_root_and_ignores_secrets_above_it()
    {
        _temp.File("secrets.json", "{}"); // above the repo root: must not be used
        _temp.Dir("repo", ".git");

        var result = SecretsFileLocator.Locate(null, _temp.Dir("repo", "src"));

        Assert.Null(result.Path);
    }

    [Fact]
    public void Worktree_without_secrets_falls_back_to_main_checkout()
    {
        _temp.Dir("main", ".git", "worktrees", "feature");
        var expected = _temp.File("main/secrets.json", "{}");
        var gitDir = Path.Combine(_temp.Root, "main", ".git", "worktrees", "feature");
        _temp.File("wt/.git", $"gitdir: {gitDir}\n");

        var result = SecretsFileLocator.Locate(null, _temp.Dir("wt", "src", "bin"));

        Assert.Equal(expected, result.Path);
    }

    [Fact]
    public void Worktree_with_relative_gitdir_falls_back_to_main_checkout()
    {
        _temp.Dir("main", ".git", "worktrees", "feature");
        var expected = _temp.File("main/secrets.json", "{}");
        _temp.File("wt/.git", "gitdir: ../main/.git/worktrees/feature\n");

        var result = SecretsFileLocator.Locate(null, _temp.Dir("wt"));

        Assert.Equal(expected, result.Path);
    }

    [Fact]
    public void Worktree_prefers_its_own_secrets_file()
    {
        _temp.Dir("main", ".git", "worktrees", "feature");
        _temp.File("main/secrets.json", "{}");
        _temp.File("wt/.git", $"gitdir: {Path.Combine(_temp.Root, "main", ".git", "worktrees", "feature")}\n");
        var expected = _temp.File("wt/secrets.json", "{}");

        var result = SecretsFileLocator.Locate(null, _temp.Dir("wt"));

        Assert.Equal(expected, result.Path);
    }

    [Fact]
    public void No_file_anywhere_returns_not_found_without_throwing()
    {
        _temp.Dir("repo", ".git");

        var result = SecretsFileLocator.Locate(null, _temp.Dir("repo", "src"));

        Assert.Null(result.Path);
        Assert.Contains("secrets.json", result.Description);
    }
}
