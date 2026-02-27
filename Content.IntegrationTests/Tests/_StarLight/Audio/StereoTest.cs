using System.Collections.Generic;
using System.Linq;
using Content.Shared.Audio;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Starlight.Audio;

[TestFixture]
public sealed class StereoTest
{
    [Test]
    public async Task TestAudioFiles()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;

        var resMan = client.ResolveDependency<IResourceManager>();
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var audioMan = client.ResolveDependency<IAudioManager>();

        var audioRoot = new ResPath("/Audio/");

        var badFiles = new Dictionary<string, string>();

        var ambienceTracks = new List<ResPath>();
        foreach (var ambience in protoMan.EnumeratePrototypes<AmbientMusicPrototype>())
        {
            switch (ambience.Sound)
            {
                case SoundCollectionSpecifier collection:
                    if (collection.Collection == null)
                        break;

                    var slothCud = protoMan.Index<SoundCollectionPrototype>(collection.Collection);
                    ambienceTracks.AddRange(slothCud.PickFiles);
                    break;
                case SoundPathSpecifier path:
                    ambienceTracks.Add(path.Path);
                    break;
            }
        }

        foreach (var file in resMan.ContentFindFiles(audioRoot))
        {
            if (ambienceTracks.Contains(file))
                continue; // Ambience tracks can be stereo, so we skip them.
            var ext = file.Extension.ToLowerInvariant();
            if (ext is not "ogg" and not "wav")
                continue;

            try
            {
                using var stream = resMan.ContentFileRead(file);
                var audioStream = ext == "ogg" ? audioMan.LoadAudioOggVorbis(stream) : audioMan.LoadAudioWav(stream);
                if (audioStream.ChannelCount != 1)
                {
                    if (audioStream.ChannelCount == 2)
                        badFiles[file.ToString()] = ($"This audio is stereo!");
                    else
                        badFiles[file.ToString()] = ($"Incorrect channels count! Channel count: {audioStream.ChannelCount}");
                }
            }
            catch (Exception e)
            {
                Assert.Fail($"Failed to read audio file {file}: {e}");
            }
        }

        Assert.That(badFiles, Is.Empty, "Some audio is invalid:\n" + string.Join('\n', badFiles.Select(p => $"{p.Key}: {p.Value}"))
        );
    }
}