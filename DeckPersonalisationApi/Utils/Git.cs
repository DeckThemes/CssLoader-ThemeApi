namespace DeckPersonalisationApi.Utils;

public class Git
{
    public string Path { get; private set; }
    private Terminal _terminal = new();

    public Git(string path)
    {
        Path = path;
        _terminal.WorkingDirectory = Path;
    }

    public static async Task Clone(string url, string workingDir)
    {
        Terminal t = new();
        t.WorkingDirectory = workingDir;
        await t.Exec("git", $"clone {url} .");

        if (t.ExitCode != 0)
            throw new Exception("Git clone failed");
    }

    private void ThrowOnInvalidErrorCode()
    {
        if (_terminal.ExitCode != 0)
            throw new Exception($"Git failed with exit code {_terminal.ExitCode}");
    }

    public async Task Pull()
    {
        await _terminal.Exec("git", "pull");
        ThrowOnInvalidErrorCode();
    }

    public async Task Fetch(string branch)
    {
        await _terminal.Exec("git", $"fetch {branch}");
        ThrowOnInvalidErrorCode();
    }

    public async Task ResetHard(string commit)
    {
        await _terminal.Exec("git", $"reset --hard {commit}");
        ThrowOnInvalidErrorCode();
    }

    public async Task Clean()
    {
        await _terminal.Exec("git", "clean -xdf");
        ThrowOnInvalidErrorCode();
    }

    public async Task Add(string path)
    {
        await _terminal.Exec("git", $"add {path}");
        ThrowOnInvalidErrorCode();
    }

    public async Task Commit(string message)
    {
        await _terminal.Exec("git", $"commit -m \"{message}\"");
        ThrowOnInvalidErrorCode();
    }

    public async Task Push(bool force = false)
    {
        await _terminal.Exec("git", "push" + (force ? " --force" : ""));
        ThrowOnInvalidErrorCode();
    }
    
    public async Task Push(string branch, bool force = false)
    {
        await _terminal.Exec("git", $"push --set-upstream origin {branch}" + (force ? " --force" : ""));
        ThrowOnInvalidErrorCode();
    }

    public async Task Checkout(string branch)
    {
        await _terminal.Exec("git", $"checkout {branch}");
        ThrowOnInvalidErrorCode();
    }

    public async Task<int> GetStagedFileCount()
    {
        await _terminal.Exec("git", "status -s");
        ThrowOnInvalidErrorCode();
        return _terminal.StdOut.Count;
    }

    public async Task<List<string>> GetBranches()
    {
        await _terminal.Exec("git", "branch");
        ThrowOnInvalidErrorCode();
        return _terminal.StdOut.Select(x => x.Replace("*", "").Trim()).ToList();
    }

    public async Task CreateBranch(string name)
    {
        List<string> branches = await GetBranches();

        if (branches.Contains(name))
            await _terminal.Exec("git", $"branch -D {name}");


        await _terminal.Exec("git", $"branch {name}");
    }

    public async Task<string> GetLatestCommitHash()
    {
        await _terminal.Exec("git", "rev-parse --short HEAD");
        ThrowOnInvalidErrorCode();
        return _terminal.StdOut.First().Trim();
    }

    public async Task<bool> DoesPullRequestExist(string branchName)
    {
        await _terminal.Exec("gh", $"pr list --json headRefName,author --head \"{branchName}\" --author \"@me\"");
        ThrowOnInvalidErrorCode();
        return _terminal.StdOut.First().Trim() != "[]";
    }

    public async Task<string> CreatePullRequest(string title, string body)
    {
        await _terminal.Exec("gh", $"pr create --title \"{title}\" --body \"{body}\"");
        ThrowOnInvalidErrorCode();
        return _terminal.StdOut.Last();
    }
}