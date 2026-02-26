using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Antags.Pirate;

/// <summary>
/// Marks an entity as a pirate. Used for status icon display.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PirateComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "PirateFaction";
}
