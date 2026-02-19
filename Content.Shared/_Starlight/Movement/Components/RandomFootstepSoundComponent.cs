using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Movement.Components;

[RegisterComponent]
public sealed partial class RandomFootstepSoundComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier? Sound;

    [DataField]
    public float Chance = 0.001f; //1/1000 steps will make this sound.
}
