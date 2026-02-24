using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Shoelaces.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TiedShoelacesStatusEffectComponent : Component
{
    [DataField]
    public float UntieSelfTime = 4.0f;

    [DataField]
    public float UntieAssistTime = 2.0f;

    [DataField]
    public float TripKnockdownTime = 1.5f;

    [DataField]
    public float TripAttemptCooldown = 0.75f;
}
