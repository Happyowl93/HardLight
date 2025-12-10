using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Antags.Vampires.Components;

[RegisterComponent]
public sealed partial class ShadowSnareTrapComponent : Component
{
    [DataField]
    public float TriggerBruteDamage = 20f;

    [DataField]
    public float MaxHealth = 200f;

    [DataField]
    public float CurrentHealth = 200f;

    [DataField]
    public SoundSpecifier? TriggerSound;

    [DataField]
    public float BreakoutTime = 5f;

    [DataField]
    public float FreeTime = 3.5f;

    [DataField]
    public float WalkSpeedMultiplier = 0.5f;

    [DataField]
    public float SprintSpeedMultiplier = 0.5f;

    [DataField]
    public int MaxEnsnares = 1;

    [DataField]
    public bool CanMoveBreakout = true;
}
