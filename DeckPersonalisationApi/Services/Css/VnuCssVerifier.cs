using System.Runtime.InteropServices;
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

    public List<string> ValidateCss(List<string> cssFiles, string workDir)
    {
        _terminal.WorkingDirectory = workDir;
        string fullWorkDirPath = Path.GetFullPath(workDir);

        // TODO: Fix. Note VNU uses HTML encoded paths
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            fullWorkDirPath = fullWorkDirPath.Replace("\\", "/");
        
        _terminal.Exec(VnuPath, $"--css --asciiquotes --verbose {string.Join(' ', cssFiles.Select(x => $"\"{x}\""))}").GetAwaiter().GetResult();

        List<string> errors = new List<string>(_terminal.StdErr).Where(x => x.Contains("error:")).Select(x => x.Replace(fullWorkDirPath, "")).ToList();
        
        if (_terminal.ExitCode != 0)
        {
            errors.ForEach(x => Console.WriteLine($"[VNU FATAL] {x}"));
        }

        return errors;
    }
}