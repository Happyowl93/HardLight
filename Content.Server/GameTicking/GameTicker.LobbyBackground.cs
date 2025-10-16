using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [ViewVariables]
    public ProtoId<LobbyBackgroundPrototype>? LobbyBackground { get; set; }

    [ViewVariables]
    private List<ProtoId<LobbyBackgroundPrototype>>? _lobbyBackgrounds;

    // STARLIGHT: Support for conditional lobby backgrounds
    private ProtoId<LobbyBackgroundPrototype>? _forcedLobbyBackground;

    private static readonly string[] WhitelistedBackgroundExtensions = new string[] {"png", "jpg", "jpeg", "webp"};

    private void InitializeLobbyBackground()
    {
        var allprotos = _prototypeManager.EnumeratePrototypes<LobbyBackgroundPrototype>().ToList();
        //create protoids from them
        foreach (var proto in allprotos)
        {
            var ext = proto.Background.Extension;
            if (WhitelistedBackgroundExtensions.Contains(ext))
            {
                //create a protoid and add it to the list
                _lobbyBackgrounds ??= new List<ProtoId<LobbyBackgroundPrototype>>();
                _lobbyBackgrounds.Add(new ProtoId<LobbyBackgroundPrototype>(proto.ID));
            }
            else
            {
                Logger.Warning($"Lobby background {proto.ID} has an invalid extension {ext}. Must be one of: {string.Join(", ", WhitelistedBackgroundExtensions)}");
            }
        }

        RandomizeLobbyBackground();
    }

    private void RandomizeLobbyBackground() {
        // STARLIGHT: Check if we have a forced background first
        if (_forcedLobbyBackground != null)
        {
            LobbyBackground = _forcedLobbyBackground;
            _forcedLobbyBackground = null; // Reset after use
            return;
        }

        if (_lobbyBackgrounds!.Any())
        {
            Logger.Info($"Choosing from {_lobbyBackgrounds!.Count} valid lobby backgrounds.");
            LobbyBackground = _robustRandom.Pick(_lobbyBackgrounds!);
            Logger.Info($"Chosen lobby background: {LobbyBackground}");
        }
        else
        {
            Logger.Error("No valid lobby backgrounds found. Make sure at least one is defined in the prototypes and has a valid image extension (png, jpg, jpeg, webp).");
            LobbyBackground = null;
        }
    }

    /// <summary>
    /// STARLIGHT: Sets a specific lobby background to be used on the next round restart.
    /// </summary>
    /// <param name="lobbyProto">The path to the background image</param>
    public void SetLobbyBackground(ProtoId<LobbyBackgroundPrototype> lobbyProto)
    {
        _forcedLobbyBackground = lobbyProto;
    }
}
