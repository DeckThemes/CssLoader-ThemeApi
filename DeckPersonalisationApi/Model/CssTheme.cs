using System.ComponentModel.DataAnnotations.Schema;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Services;

namespace DeckPersonalisationApi.Model;

public class CssTheme : IToDto<PartialCssThemeDto>, IToDto<MinimalCssThemeDto>, IToDto<FullCssThemeDto>
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? DisplayName { get; set; }
    public ThemeType Type { get; set; }
    public List<SavedBlob> Images { get; set; }
    public SavedBlob Download { get; set; }
    public string Version { get; set; }
    public string? Source { get; set; }
    public User Author { get; set; }
    public string SpecifiedAuthor { get; set; }
    public DateTimeOffset Submitted { get; set; }
    public DateTimeOffset Updated { get; set; }
    public string Target { get; set; }
    public long Targets { get; set; }
    public int ManifestVersion { get; set; }
    public string Description { get; set; }
    [InverseProperty("DependenciesOf")]
    public List<CssTheme> Dependencies { get; set; }
    [InverseProperty("Dependencies")]
    public List<CssTheme> DependenciesOf { get; set; }
    public PostVisibility Visibility { get; set; }
    public ICollection<User> UserStars { get; set; }
    public long StarCount { get; set; }

    public PartialCssThemeDto ToDto()
        => new(this);

    MinimalCssThemeDto IToDto<MinimalCssThemeDto>.ToDto()
        => new(this);

    FullCssThemeDto IToDto<FullCssThemeDto>.ToDto()
        => new(this);

    public object ToDtoObject()
        => ToDto();

    public List<string> ToReadableTargets()
    {
        List<string> targets = (Type == ThemeType.Css) ? AppConfiguration.CssTargets : AppConfiguration.AudioTargets;
        long n = Targets;
        List<string> resolvedTargets = new();
        for (int i = 0; i < targets.Count; i++)
        {
            long t = (n >> i) & 0x1;
            if (t == 1)
            {
                resolvedTargets.Add(targets[i]);
            }
        }

        return resolvedTargets;
    }

    public static long ToBitfieldTargets(List<string> appliedTargets)
    {
        return ToBitfieldTargets(appliedTargets, ThemeType.Css) | ToBitfieldTargets(appliedTargets, ThemeType.Audio);
    }
    
    public static long ToBitfieldTargets(List<string> appliedTargets, ThemeType type)
    {
        List<string> allTargets = (type == ThemeType.Css) ? AppConfiguration.CssTargets : AppConfiguration.AudioTargets;
        appliedTargets = appliedTargets.Distinct().ToList();
        long n = 0;
        
        for (int i = 0; i < allTargets.Count; i++)
        {
            if (appliedTargets.Contains(allTargets[i]))
            {
                n |= 1 << 1;
            }
        }

        return n;
    }

    public static long ToBitfieldTargets(string target, ThemeType type)
        => ToBitfieldTargets(new List<string>() { target }, type);
}