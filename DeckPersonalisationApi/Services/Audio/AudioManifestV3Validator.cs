using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Audio;

public class AudioManifestV3Validator : AudioManifestV2Validator
{
    public AudioManifestV3Validator(string packPath, JObject json, User user, List<string> validFiles) : base(packPath, json, user, validFiles)
    {
        ManifestVersion = 3;
    }

    protected override void VerifyIntroMusic()
    {
        if ((File.Exists("intro_music.mp3") || Mappings.ContainsKey("intro_music.mp3")) && !Music)
            throw new Exception($"Intro music is only supported in music packs");
    }
}