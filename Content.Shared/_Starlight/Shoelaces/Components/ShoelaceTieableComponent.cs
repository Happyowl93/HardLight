using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Shoelaces.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShoelaceTieableComponent : Component
{
    [DataField]
    public float TieTime = 5.0f;

    [DataField]
    public EntProtoId TiedStatusEffect = "StatusEffectTiedShoelaces";

    [DataField]
    public TimeSpan TiedDuration = TimeSpan.FromSeconds(45);
}
