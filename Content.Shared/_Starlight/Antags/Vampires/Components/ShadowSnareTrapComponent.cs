using Robust.Shared.GameObjects;

namespace Content.Shared._Starlight.Antags.Vampires.Components;

[RegisterComponent]
public sealed partial class ShadowSnareTrapComponent : Component
{
    [DataField]
    public float ShadowSnareTriggerBrute = 20f;

    [DataField]
    public float ShadowSnareSlowMultiplier = 0.5f;
}
