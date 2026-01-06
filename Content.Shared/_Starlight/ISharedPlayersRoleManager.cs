using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Player;

namespace Content.Shared.Starlight;

public interface ISharedPlayersRoleManager
{

    PlayerData? GetPlayerData(EntityUid uid);
    PlayerData? GetPlayerData(ICommonSession session);

    int? GetBalance(EntityUid uid);
    int? GetBalance(ICommonSession session);

    void SetBalance(EntityUid uid, int value, bool skipNullLink = false);
    void SetBalance(ICommonSession session, int value, bool skipNullLink = false);
}
