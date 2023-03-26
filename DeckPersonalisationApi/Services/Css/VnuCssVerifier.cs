using System.Runtime.InteropServices;
using DeckPersonalisationApi.Utils;

namespace DeckPersonalisationApi.Services.Css;

public class VnuCssVerifier
{
    private AppConfiguration _config;
    private Terminal _terminal = new();
    
    public VnuCssVerifier(AppConfiguration config)
    {
        _config = config;
        //_terminal.Silence = true;
    }

    public List<string> ValidateCss(List<string> cssFiles, string workDir, List<string> extraErrors)
    {
        if (cssFiles.Count <= 0)
            return new();
        
        _terminal.WorkingDirectory = workDir;
        string fullWorkDirPath = Path.GetFullPath(workDir);

        // TODO: Fix. Note VNU uses HTML encoded paths
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            fullWorkDirPath = fullWorkDirPath.Replace("\\", "/");
        
        _terminal.Exec(_config.VnuPath, new List<string>{"--css", "--asciiquotes", "--verbose"}.Concat(cssFiles).ToList()).GetAwaiter().GetResult();

        List<string> errors = new List<string>(_terminal.StdErr).Where(x => x.Contains("error:")).Select(x => x.Replace(fullWorkDirPath, "")).ToList();
        
        if (_terminal.ExitCode != 0)
        {
            if (_terminal.ExitCode == -69420)
                errors.Add("VNU is not installed. Unable to validate CSS");
            
            errors.ForEach(x => Console.WriteLine($"[VNU FATAL] {x}"));
        }
        
        errors.AddRange(extraErrors);

        return errors;
    }
}
