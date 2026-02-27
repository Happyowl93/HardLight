using System.Collections.Generic;
using System.Linq;
using Content.Shared.Audio;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Starlight.Audio;

[TestFixture]
public sealed class StereoTest
{
    // Mark specific files as ignored if they not using positioning, for example: Ambience, Announcements.
    public List<ResPath> IgnoredFiles = [];

    public List<ResPath> IgnoredPaths = [
            new ResPath("/Audio/Announcements"), // Announcements can be stereo because they don't have positioning.
        ];

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

            if (IgnoredFiles.Contains(file) || IgnoredPaths.Any(p => file.ToString().StartsWith(p.ToString()))) // We can ignore some files/paths if we want to, for example if they are stereo on purpose or if we just don't care about them.
                continue;

            var ext = file.Extension.ToLowerInvariant();
            if (ext is not "ogg" and not "wav")
                continue;

            try
            {
                using var stream = resMan.ContentFileRead(file);
                var audioStream = ext == "ogg" ? audioMan.LoadAudioOggVorbis(stream) : audioMan.LoadAudioWav(stream);
                if (audioStream.ChannelCount != 1)
                {
                    badFiles[file.ToString()] = audioStream.ChannelCount == 2
                        ? $"This audio is STEREO but NEEDS to be MONO!"
                        : $"Incorrect channels count! Channel count: {audioStream.ChannelCount}, but it should have only 1 channel.";
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