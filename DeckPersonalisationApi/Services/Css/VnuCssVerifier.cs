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
        _terminal.Silence = true;
    }

    public bool ValidateCss(List<string> cssFiles, string workDir)
    {
        _terminal.WorkingDirectory = workDir;
        _terminal.Exec(VnuPath, $"--css --asciiquotes --verbose {string.Join(' ', cssFiles.Select(x => $"\"{x}\""))}").GetAwaiter().GetResult();

        if (_terminal.ExitCode != 0)
        {
            List<string> errors = new(_terminal.StdErr);
            errors = errors.Where(x => x.Contains("error:")).ToList();
            
            errors.ForEach(x => Console.WriteLine($"[VNU FATAL] {x}"));
        }
        
        return _terminal.ExitCode == 0;
    }
}