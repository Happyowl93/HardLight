using System.Threading.Tasks;
using Content.Shared._NullLink;
using Robust.Shared.Player;
using Starlight.NullLink.Event;

namespace Content.Server._NullLink.PlayerData;

public sealed partial class NullLinkPlayerManager : INullLinkPlayerManager
{
    public ValueTask UpdateResource(ResourceChangedEvent ev)
    {
        if (!_playerById.TryGetValue(ev.Player, out var playerData))
            return ValueTask.CompletedTask;
        playerData.Resources[ev.Resource] = ev.NewAmount;

        SendPlayerResources(playerData.Session, playerData.Resources);
        return ValueTask.CompletedTask;
    }

    private void SendPlayerResources(ICommonSession session, Dictionary<string, double> resources)
        => _netMgr.ServerSendMessage(new MsgUpdatePlayerResources
        {
            Resources = resources
        }, session.Channel);
}