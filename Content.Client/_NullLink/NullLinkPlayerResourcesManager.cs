using System.Diagnostics.CodeAnalysis;
using Content.Shared._NullLink;
using Robust.Shared.Network;

namespace Content.Client._NullLink;

public sealed class NullLinkPlayerResourcesManager : INullLinkPlayerResourcesManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private Dictionary<string, double> _resources = [];
    private ISawmill _sawmill = default!;

    public event Action PlayerResourcesChanged = delegate { };


    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("_null.resources");
        _netMgr.RegisterNetMessage<MsgUpdatePlayerResources>(Update);
    }

    private void Update(MsgUpdatePlayerResources message)
    {
        _resources = message.Resources;

        _sawmill.Info("Updated player resources");
        PlayerResourcesChanged?.Invoke();
    }

    public bool TryGetResource(string id, [NotNullWhen(true)] out double? value)
    {
        value = null;
        if (!_resources.TryGetValue(id, out var Value))
            return false;
        
        value = Value;
        return true;
    }
}