using DeckPersonalisationApi.Utils;

namespace DeckPersonalisationApi.Services.Css;

public class VnuCssVerifier
{
    private IConfiguration _config;
    private Terminal _terminal = new();
    
    public string VnuPath => _config["Config:VnuPath"]!;
    
    public VnuCssVerifier(IConfiguration config)
    {
        _config = config;
    }

    public bool ValidateCss(List<string> cssFiles, string workDir)
    {
        _terminal.WorkingDirectory = workDir;
        _terminal.Exec(VnuPath, $"--css --asciiquotes --verbose {string.Join(' ', cssFiles.Select(x => $"\"{x}\""))}").GetAwaiter().GetResult();
        return _terminal.ExitCode == 0;
    }
}