using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HemomancerComponent : Component
{
    [AutoNetworkedField]
    public bool InSanguinePool = false;
    [AutoNetworkedField]
    public bool BloodBringersRiteActive = false;
    public Dictionary<string, int>? PoolOriginalMasks = null;
    public Dictionary<string, int>? PoolOriginalLayers = null;
    public bool PoolOwnedGodmode = false;

    public int BloodBringersRiteLoopId = 0;
}